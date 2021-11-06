using System;
using System.Collections;
using System.Threading.Tasks;
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
	public class DateTimeTest
	{
		class TestObj
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Name { get; set; }
			public DateTime ModifiedTime { get; set; }
		}


		[Test]
		public void AsTicks ()
		{
			var db = new TestDb (storeDateTimeAsTicks: true);
			TestDateTime (db);
		}

		[Test]
		public void AsStrings ()
		{
			var db = new TestDb (storeDateTimeAsTicks: false);
			TestDateTime (db);
		}

		[TestCase ("o")]
		[TestCase ("MMM'-'dd'-'yyyy' 'HH':'mm':'ss'.'fffffff")]
		public void AsCustomStrings (string format)
		{
			var db = new TestDb (CustomDateTimeString (format));
			TestDateTime (db);
		}

		[UnityTest]
		public IEnumerator AsyncAsTicks() => UniTask.ToCoroutine(async () =>
		{
			var db = new SQLiteAsyncConnection(TestPath.GetTempFileName(), true);
			await TestAsyncDateTime(db);
		});

		[UnityTest]
		public IEnumerator AsyncAsString() => UniTask.ToCoroutine(async () =>
		{
			var db = new SQLiteAsyncConnection(TestPath.GetTempFileName(), false);
			await TestAsyncDateTime(db);
		});

		[UnityTest]
		public IEnumerator AsyncAsCustomStrings() => UniTask.ToCoroutine(async () =>
		{
			await AsyncAsCustomStrings("o");
			await AsyncAsCustomStrings("MMM'-'dd'-'yyyy' 'HH':'mm':'ss'.'fffffff");

		});
		
		public async UniTask AsyncAsCustomStrings(string format)
		{
			var db = new SQLiteAsyncConnection(CustomDateTimeString(format));
			await TestAsyncDateTime(db);
		}
		
		SQLiteConnectionString CustomDateTimeString (string dateTimeFormat) => new SQLiteConnectionString (TestPath.GetTempFileName (), false, dateTimeFormat);

		async UniTask TestAsyncDateTime (SQLiteAsyncConnection db)
		{
			await db.CreateTableAsync<TestObj>();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
				ModifiedTime = new DateTime (2012, 1, 14, 3, 2, 1, 234),
			};
			await db.InsertAsync(o);
			o2 = await db.GetAsync<TestObj>(o.Id);
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
		}

		void TestDateTime (TestDb db)
		{
			db.CreateTable<TestObj> ();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
				ModifiedTime = new DateTime (2012, 1, 14, 3, 2, 1, 234),
			};
			db.Insert (o);
			o2 = db.Get<TestObj> (o.Id);
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
		}

		class NullableDateObj
		{
			public DateTime? Time { get; set; }
		}

		[UnityTest]
		public IEnumerator LinqNullable() => UniTask.ToCoroutine(async () =>
		{
			foreach (var option in new[] {true, false})
			{
				var db = new SQLiteAsyncConnection(TestPath.GetTempFileName(), option);
				await db.CreateTableAsync<NullableDateObj>();

				var epochTime = new DateTime(1970, 1, 1);

				await db.InsertAsync(new NullableDateObj {Time = epochTime});
				await db.InsertAsync(new NullableDateObj {Time = new DateTime(1980, 7, 23)});
				await db.InsertAsync(new NullableDateObj {Time = null});
				await db.InsertAsync(new NullableDateObj {Time = new DateTime(2019, 1, 23)});

				var res = await db.Table<NullableDateObj>().Where(x => x.Time == epochTime).ToListAsync();
				Assert.AreEqual(1, res.Count);

				res = await db.Table<NullableDateObj>().Where(x => x.Time > epochTime).ToListAsync();
				Assert.AreEqual(2, res.Count);
			}
		});
	}
}

