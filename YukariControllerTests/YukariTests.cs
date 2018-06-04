using Microsoft.VisualStudio.TestTools.UnitTesting;
using YukariController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController.Tests
{
    [TestClass()]
    public class YukariTests
    {
        [TestMethod()]
        public async Task SaveTest()
        {
            var yukari = new Yukari();
            var msg = "bbbbb";
            var ran = new Random();
            await yukari.Save(msg,  ran.Next(1000).ToString());
        }
    }
}