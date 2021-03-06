﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NetMQ.Sockets;
using NetMQ.zmq;

namespace NetMQ.Tests
{
	[TestFixture]
	public class SocketTests
	{
		[Test, ExpectedException(typeof(AgainException))]
		public void CheckRecvAgainException()
		{
			using (NetMQContext context = NetMQContext.Create())
			{
				using (var routerSocket = context.CreateRouterSocket())
				{
					routerSocket.Bind("tcp://127.0.0.1:5555");

					routerSocket.Receive(SendReceiveOptions.DontWait);
				}
			}
		}

		[Test, ExpectedException(typeof(AgainException))]
		public void CheckSendAgainException()
		{
			using (NetMQContext context = NetMQContext.Create())
			{
				using (var routerSocket = context.CreateRouterSocket())
				{
					routerSocket.Bind("tcp://127.0.0.1:5555");
					routerSocket.Options.Linger = TimeSpan.Zero;

					using (var dealerSocket = context.CreateDealerSocket())
					{
						dealerSocket.Options.SendHighWatermark = 1;
						dealerSocket.Options.Linger = TimeSpan.Zero;
						dealerSocket.Connect("tcp://127.0.0.1:5555");

						dealerSocket.Send("1", true, false);
						dealerSocket.Send("2", true, false);
					}
				}
			}
		}


		[Test]
		public void TestKeepAlive()
		{
			// there is no way to test tcp keep alive without disconnect the cable, we just testing that is not crashing the system
			using (NetMQContext context = NetMQContext.Create())
			{
				using (var responseSocket = context.CreateResponseSocket())
				{
					responseSocket.Options.TcpKeepalive = true;
					responseSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(5);
					responseSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(1);

					responseSocket.Bind("tcp://127.0.0.1:5555");

					using (var requestSocket = context.CreateRequestSocket())
					{
						requestSocket.Options.TcpKeepalive = true;
						requestSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(5);
						requestSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(1);

						requestSocket.Connect("tcp://127.0.0.1:5555");

						requestSocket.Send("1");

						bool more;
						string m = responseSocket.ReceiveString(out more);

						Assert.IsFalse(more);
						Assert.AreEqual("1", m);

						responseSocket.Send("2");

						string m2 = requestSocket.ReceiveString(out more);

						Assert.IsFalse(more);
						Assert.AreEqual("2", m2);

						Assert.IsTrue(requestSocket.Options.TcpKeepalive);
						Assert.AreEqual(TimeSpan.FromSeconds(5), requestSocket.Options.TcpKeepaliveIdle);
						Assert.AreEqual(TimeSpan.FromSeconds(1), requestSocket.Options.TcpKeepaliveInterval);

						Assert.IsTrue(responseSocket.Options.TcpKeepalive);
						Assert.AreEqual(TimeSpan.FromSeconds(5), responseSocket.Options.TcpKeepaliveIdle);
						Assert.AreEqual(TimeSpan.FromSeconds(1), responseSocket.Options.TcpKeepaliveInterval);
					}
				}
			}
		}
	}
}
