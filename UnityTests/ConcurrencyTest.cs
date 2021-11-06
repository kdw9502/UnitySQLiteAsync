using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TearDown = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestCleanupAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
    [TestFixture]
    public class ConcurrencyTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        public class DbReader
        {
            private CancellationToken cancellationToken;

            public DbReader(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public UniTask Run()
            {
                var t = UniTask.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
							//
							// NOTE: Change this to readwrite and then it does work ???
							// No more IOERROR
							// 

							var flags = SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadOnly;
#if __IOS__
							flags = SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite;
#endif
							using (var dbConnection = new DbConnection(flags))
                            {
                                var records = dbConnection.Table<TestObj>().ToList();
                                System.Diagnostics.Debug.WriteLine($"{Environment.CurrentManagedThreadId} Read records: {records.Count}");
                            }

                            // sqlite-net : (No await so we stay on the same thread) but UniTask use same thread 
                            await UniTask.Delay(10, cancellationToken: cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });

                return t;
            }

        }

        public class DbWriter
        {
            private CancellationToken cancellationToken;

            public DbWriter(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public UniTask Run()
            {
                var t = UniTask.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            using (var dbConnection = new DbConnection(SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite))
                            {
                                System.Diagnostics.Debug.WriteLine($"{Environment.CurrentManagedThreadId} Start insert");

                                for (var i = 0; i < 50; i++)
                                {
                                    var newRecord = new TestObj()
                                    {
                                    };

                                    dbConnection.Insert(newRecord);
                                }

                                System.Diagnostics.Debug.WriteLine($"{Environment.CurrentManagedThreadId} Inserted records");
                            }

                            // sqlite-net : (No await so we stay on the same thread) but UniTask use same thread
                            await UniTask.Delay(1, cancellationToken: cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });

                return t;
            }

        }

        public class DbConnection : SQLiteConnection
        {
            private static string DbPath = GetTempFileName();

            private static string GetTempFileName()
            {
#if NETFX_CORE
                var name = Guid.NewGuid() + ".sqlite";
                return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, name);
#else
                return TestPath.GetTempFileName();
#endif
            }


            public DbConnection(SQLiteOpenFlags openflags) : base(DbPath, openflags)
            {
                this.BusyTimeout = TimeSpan.FromSeconds(5);
            }

            public void CreateTables()
            {
                CreateTable<TestObj>();
            }
        }

        [SetUp]
        public void Setup()
        {
            using (var dbConenction = new DbConnection(SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create))
            {
                dbConenction.CreateTables();
            }
        }


        [UnityTest]
        public IEnumerator TestLoad() => UniTask.ToCoroutine(async () =>
        {
            try
            {

                var tokenSource = new CancellationTokenSource();
                var tasks = new List<UniTask>();
                tasks.Add(new DbReader(tokenSource.Token).Run());
                tasks.Add(new DbWriter(tokenSource.Token).Run());

                // Wait 5sec
                tokenSource.CancelAfter(5000);

                await UniTask.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        });

        /// <summary>
        /// Test for issue #761. Because the nature of this test is a race condition,
        /// it is not guaranteed to fail if the issue is present. It does appear to
        /// fail most of the time, though.
        /// </summary>
        [UnityTest]
        public IEnumerator TestInsertCommandCreation() => UniTask.ToCoroutine(async () =>
        {
            using (var dbConnection =
                new DbConnection(SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create))
            {
                var obj1 = new TestObj();
                var obj2 = new TestObj();
                var taskA = UniTask.Run(() => { dbConnection.Insert(obj1); });
                var taskB = UniTask.Run(() => { dbConnection.Insert(obj2); });

                await UniTask.WhenAll(taskA, taskB);
            }
        });
    }
}

