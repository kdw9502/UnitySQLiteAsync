using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
	[TestFixture]
	public class DateTimeOffsetTest
	{
		class TestObj
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Name { get; set; }
			public DateTimeOffset ModifiedTime { get; set; }
		}


		[Test]
		public void AsTicks ()
		{
			var db = new TestDb ();
			TestDateTimeOffset (db);
		}


		[UnityTest]
		public IEnumerator AsyncAsTicks() => UniTask.ToCoroutine(async () =>
		{
			var db = new SQLiteAsyncConnection(TestPath.GetTempFileName());
			await TestAsyncDateTimeOffset(db);
		});

		async UniTask TestAsyncDateTimeOffset (SQLiteAsyncConnection db)
		{
			await db.CreateTableAsync<TestObj>();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
                ModifiedTime = new DateTimeOffset (2012, 1, 14, 3, 2, 1, TimeSpan.Zero),
			};
			await db.InsertAsync(o);
			o2 = await db.GetAsync<TestObj>(o.Id);
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
		}

		void TestDateTimeOffset (TestDb db)
		{
			db.CreateTable<TestObj> ();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
				ModifiedTime = new DateTimeOffset (2012, 1, 14, 3, 2, 1, TimeSpan.Zero),
			};
			db.Insert (o);
			o2 = db.Get<TestObj> (o.Id);
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
		}

	}
}

