using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
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
	public class TransactionTest
	{
		private TestDb db;
		private List<TestObj> testObjects;

		public class TestObj
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public override string ToString()
			{
				return string.Format("[TestObj: Id={0}]", Id);
			}
		}

		public class TransactionTestException : Exception
		{
		}

		public class TestDb : SQLiteConnection
		{
			public TestDb(String path) : base(path)
			{
				CreateTable<TestObj>();
			}
		}

		[SetUp]
		public void Setup()
		{
			testObjects = Enumerable.Range(1, 20).Select(i => new TestObj()).ToList();

			db = new TestDb(TestPath.GetTempFileName());
			db.InsertAll(testObjects);
		}

		[TearDown]
		public void TearDown()
		{
			if (db != null) {
				db.Close();
			}
		}

		[Test]
		public void SuccessfulSavepointTransaction()
		{
			db.RunInTransaction(() => {
				db.Delete(testObjects[0]);
				db.Delete(testObjects[1]);
				db.Insert(new TestObj());
			});

			Assert.AreEqual(testObjects.Count - 1, db.Table<TestObj>().Count());
		}

		[Test]
		public void FailSavepointTransaction()
		{
			try {
				db.RunInTransaction(() => {
					db.Delete(testObjects[0]);

					throw new TransactionTestException();
				});
			} catch (TransactionTestException) {
				// ignore
			}

			Assert.AreEqual(testObjects.Count, db.Table<TestObj>().Count());
		}

		[Test]
		public void SuccessfulNestedSavepointTransaction()
		{
			db.RunInTransaction(() => {
				db.Delete(testObjects[0]);

				db.RunInTransaction(() => {
					db.Delete(testObjects[1]);
				});
			});

			Assert.AreEqual(testObjects.Count - 2, db.Table<TestObj>().Count());
		}

		[Test]
		public void FailNestedSavepointTransaction()
		{
			try {
				db.RunInTransaction(() => {
					db.Delete(testObjects[0]);

					db.RunInTransaction(() => {
						db.Delete(testObjects[1]);

						throw new TransactionTestException();
					});
				});
			} catch (TransactionTestException) {
				// ignore
			}

			Assert.AreEqual(testObjects.Count, db.Table<TestObj>().Count());
		}

		[UnityTest]
		public IEnumerator Issue329_AsyncTransactionFailuresShouldRollback() => UniTask.ToCoroutine(async () =>
		{
			var adb = new SQLiteAsyncConnection(TestPath.GetTempFileName());
			await adb.CreateTableAsync<TestObj>();
			var initialCount = await adb.Table<TestObj>().CountAsync();
			var rollbacks = 0;

			//
			// Fail a commit
			//
			adb.Trace = true;
			adb.Tracer = m =>
			{
				Console.WriteLine(m);
				if (m == "Executing: rollback")
					rollbacks++;
			};

			LogAssert.Expect(LogType.Exception, "Exception: User exception");
			try
			{
				await adb.RunInTransactionAsync(db =>
				{
					db.Insert(new TestObj());
					throw new Exception("User exception");
				});
				Assert.Fail("Should have thrown");
			}
			catch (AggregateException aex)
				when (aex.InnerException.Message == "User exception")
			{
				// Expected
			}
			LogAssert.NoUnexpectedReceived();

			Assert.AreEqual(1, rollbacks);
		});

		[UnityTest]
		public IEnumerator Issue604_RunInTransactionAsync() => UniTask.ToCoroutine(async () =>
		{
			var adb = new SQLiteAsyncConnection(TestPath.GetTempFileName());
			await adb.CreateTableAsync<TestObj>();
			var initialCount = await adb.Table<TestObj>().CountAsync();

			//
			// Fail a commit
			//
			adb.Trace = true;
			adb.Tracer = m =>
			{
				//Console.WriteLine (m);
				if (m.Trim().EndsWith("commit"))
					throw SQLiteException.New(SQLite3.Result.Busy, "Make commit fail");
			};

			// UnityTestRunner can't handle Exception properly.
			LogAssert.Expect(LogType.Exception, "SQLiteException: Make commit fail");
			try
			{
				await adb.RunInTransactionAsync(db => { db.Insert(new TestObj()); });
				Assert.Fail("Should have thrown");
			}
			catch (AggregateException aex)
				when (aex.InnerException is SQLiteException ex
				      && ex.Result == SQLite3.Result.Busy)
			{
				// Expected
			}
			LogAssert.NoUnexpectedReceived();


			//
			// Are we stuck?
			//
			adb.Tracer = null;
			await adb.RunInTransactionAsync(db => { db.Insert(new TestObj()); });

			Assert.AreEqual(initialCount + 1, await adb.Table<TestObj>().CountAsync());
		});

		[Test]
		public void Issue604_RecoversFromFailedCommit ()
		{
			db.Trace = true;
			var initialCount = db.Table<TestObj> ().Count ();

			//
			// Well this is an issue because there is an internal variable called _transactionDepth
			// that tries to track if we are in an active transaction.
			// The problem is, _transactionDepth is set to 0 and then commit is executed on the database.
			// Well, the commit fails and "When COMMIT fails in this way, the transaction remains active and
			// the COMMIT can be retried later after the reader has had a chance to clear"
			//
			var rollbacks = 0;
			db.Tracer = m => {
				if (m == "Executing: commit")
					throw SQLiteException.New (SQLite3.Result.Busy, "Make commit fail");
				if (m == "Executing: rollback")
					rollbacks++;
			};
			db.BeginTransaction ();
			db.Insert (new TestObj ());
			try {
				db.Commit ();
				Assert.Fail ("Should have thrown");
			}
			catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Busy) {
				db.Tracer = null;
			}
			Assert.False (db.IsInTransaction);
			Assert.AreEqual (1, rollbacks);

			//
			// The catch statements in the RunInTransaction family of functions catch this and call rollback,
			// but since _transactionDepth is 0, the transaction isn't actually rolled back.
			//
			// So the next time begin transaction is called on the same connection,
			// sqlite-net attempts to begin a new transaction (because _transactionDepth is 0),
			// which promptly fails because there is still an active transaction on the connection.
			//
			// Well now we are in big trouble because _transactionDepth got set to 1,
			// and when begin transaction fails in this manner, the transaction isn't rolled back
			// (which would have set _transactionDepth to 0)
			//
			db.BeginTransaction ();
			db.Insert (new TestObj ());
			db.Commit ();
			Assert.AreEqual (initialCount + 1, db.Table<TestObj> ().Count ());
		}

		[Test]
		public void Issue604_RecoversFromFailedRelease ()
		{
			db.Trace = true;
			var initialCount = db.Table<TestObj> ().Count ();

			var rollbacks = 0;
			db.Tracer = m => {
				//Console.WriteLine (m);
				if (m.StartsWith ("Executing: release"))
					throw SQLiteException.New (SQLite3.Result.Busy, "Make release fail");
				if (m == "Executing: rollback")
					rollbacks++;
			};
			var sp0 = db.SaveTransactionPoint ();
			db.Insert (new TestObj ());
			try {
				db.Release (sp0);
				Assert.Fail ("Should have thrown");
			}
			catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Busy) {
				db.Tracer = null;
			}
			Assert.False (db.IsInTransaction);
			Assert.AreEqual (1, rollbacks);

			db.BeginTransaction ();
			db.Insert (new TestObj ());
			db.Commit ();
			Assert.AreEqual (initialCount + 1, db.Table<TestObj> ().Count ());
		}
	}
}

