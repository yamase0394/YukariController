using Microsoft.VisualStudio.TestTools.UnitTesting;
using YukariController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace YukariController.Tests
{
    [TestClass()]
    public class YukariTests
    {
        [TestMethod()]
        public async Task CancelTest()
        {
            Console.WriteLine("start");
            var yukariManager = new YukariManager();
            var testServer = new LocalServer(yukariManager.GetDispatcher());

            Console.WriteLine("create tasks");
            var tasks = new List<Task>();
            for(int i = 0; i < 5; i++)
            {
                var task = Task.Run(async() =>
                {
                    var result = await testServer.EnqueueMessage(YukariManager.Command.Play, "aaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                    Console.WriteLine($"{result}");
                });
                tasks.Add(task);
            }

            await Task.Delay(1000);
            var cancelResult = await testServer.EnqueueMessage(YukariManager.Command.Stop, null);
            Console.WriteLine($"{cancelResult}");

            await Task.WhenAll(tasks);
        }
    }
}