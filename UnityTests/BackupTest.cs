using System.Collections;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;

namespace SQLite.Tests
{
	[TestFixture]
	public class BackupTest
	{
		[UnityTest]
		public IEnumerator BackupOneTable() => UniTask.ToCoroutine(async ()=>
		{
			var pathSrc = TestPath.GetTempFileName();
			var pathDest = TestPath.GetTempFileName();

			var db = new SQLiteAsyncConnection(pathSrc);
			await db.CreateTableAsync<OrderLine>();
			await db.InsertAsync(new OrderLine { });
			var lines = await db.Table<OrderLine>().ToListAsync();
			Assert.AreEqual(1, lines.Count);

			await db.BackupAsync(pathDest);

			var destLen = new FileInfo(pathDest).Length;
			Assert.True(destLen >= 4096);
		});
}
}