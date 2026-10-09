using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Session_ME : ISession
{
	private static readonly object networkLock = new object();
	private static volatile int networkGeneration;
	public class Sender
	{
		public List<Message> sendingMessage;

		public Sender()
		{
			sendingMessage = new List<Message>();
		}

		public void AddMessage(Message message)
		{
			lock (sendingMessage) sendingMessage.Add(message);
		}

		public void run()
		{
			run(networkGeneration);
		}

		public void run(int generation)
		{
			while (connected && generation == networkGeneration)
			{
				try
				{
					if (getKeyComplete)
					{
						while (connected)
						{
							lock (networkLock)
							{
								if (generation != networkGeneration || !connected) return;
								Message m;
								lock (sendingMessage)
								{
									if (sendingMessage.Count == 0) break;
									m = sendingMessage[0];
									sendingMessage.RemoveAt(0);
								}
								doSendMessage(m);
							}
						}
					}
					try
					{
						Thread.Sleep(5);
					}
					catch (Exception ex)
					{
						Cout.LogError(ex.ToString());
					}
				}
				catch (Exception)
				{
					Res.outz("error send message! ");
				}
			}
		}
	}

	private class MessageCollector
	{
		private readonly int generation = networkGeneration;
		private readonly BinaryReader dis = Session_ME.dis;
		// A cancelled collector must never advance the next connection's cipher cursor.
		private sbyte[] receiveKey;
		private int receiveKeyIndex;

		private sbyte readKey(sbyte value)
		{
			sbyte decoded = (sbyte)((receiveKey[receiveKeyIndex] & 0xFF) ^ (value & 0xFF));
			receiveKeyIndex = (receiveKeyIndex + 1) % receiveKey.Length;
			return decoded;
		}
		public void run()
		{
			try
			{
				while (connected && generation == networkGeneration)
				{
					Message message = readMessage();
					if (message == null)
					{
						break;
					}
					try
					{
						lock (networkLock)
						{
							if (generation != networkGeneration) return;
							if (message.command == -27)
							{
								getKey(message);
							}
							else
							{
								onRecieveMsg(message);
							}
						}
					}
					catch (Exception)
					{
						Cout.println("LOI NHAN  MESS THU 1");
					}
					try
					{
						Thread.Sleep(5);
					}
					catch (Exception)
					{
						Cout.println("LOI NHAN  MESS THU 2");
					}
				}
			}
			catch (Exception ex3)
			{
				Debug.Log("error read message!");
				Debug.Log(ex3.Message.ToString());
			}
			lock (networkLock)
			{
				if (!connected || generation != networkGeneration)
				{
					return;
				}
				if (messageHandler != null)
				{
					if (currentTimeMillis() - timeConnected > 500)
					{
						messageHandler.onDisconnected(isMainSession);
					}
					else
					{
						messageHandler.onConnectionFail(isMainSession);
					}
				}
				if (sc != null)
				{
					cleanNetwork();
				}
			}
		}

		private void getKey(Message message)
		{
			try
			{
				sbyte b = message.reader().readSByte();
				key = new sbyte[b];
				for (int i = 0; i < b; i++)
				{
					key[i] = message.reader().readSByte();
				}
				for (int j = 0; j < key.Length - 1; j++)
				{
					ref sbyte reference = ref key[j + 1];
					reference = (sbyte)(reference ^ key[j]);
				}
				receiveKey = key;
				getKeyComplete = true;
				GameMidlet.IP2 = message.reader().readUTF();
				GameMidlet.PORT2 = message.reader().readInt();
				GameMidlet.isConnect2 = ((message.reader().readByte() != 0) ? true : false);
				if (isMainSession && GameMidlet.isConnect2)
				{
					GameCanvas.connect2();
				}
			}
			catch (Exception)
			{
			}
		}

		private Message readMessage2(sbyte cmd)
		{
			int num = readKey(dis.ReadSByte()) + 128;
			int num2 = readKey(dis.ReadSByte()) + 128;
			int num3 = readKey(dis.ReadSByte()) + 128;
			int num4 = (num3 * 256 + num2) * 256 + num;
			sbyte[] array = new sbyte[num4];
			byte[] src = dis.ReadBytes(num4);
			//Buffer.BlockCopy(src, 0, array, 0, num4);
			array = ArrayCast.cast(src);
			recvByteCount += 5 + num4;
			int num6 = recvByteCount + sendByteCount;
			strRecvByteCount = num6 / 1024 + "." + num6 % 1024 / 102 + "Kb";
			if (receiveKey != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = readKey(array[i]);
				}
			}
			return new Message(cmd, array);
		}

		private Message readMessage()
		{
			try
			{
				sbyte b = dis.ReadSByte();
				if (receiveKey != null)
				{
					b = readKey(b);
				}
				if (b == -32 || b == -66 || b == 11 || b == -67 || b == -74 || b == -87 || b == 66)
				{
					return readMessage2(b);
				}
				int num;
				if (receiveKey != null)
				{
					sbyte b2 = dis.ReadSByte();
					sbyte b3 = dis.ReadSByte();
					num = ((readKey(b2) & 0xFF) << 8) | (readKey(b3) & 0xFF);
				}
				else
				{
					sbyte b4 = dis.ReadSByte();
					sbyte b5 = dis.ReadSByte();
					num = (b4 & 0xFF00) | (b5 & 0xFF);
				}
				sbyte[] array = new sbyte[num];
				int num2 = 0;
				int num3 = 0;
				byte[] src = dis.ReadBytes(num);
				//Buffer.BlockCopy(src, 0, array, 0, num);
                array = ArrayCast.cast(src);
                recvByteCount += 5 + num;
				int num4 = recvByteCount + sendByteCount;
				strRecvByteCount = num4 / 1024 + "." + num4 % 1024 / 102 + "Kb";
				if (receiveKey != null)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = readKey(array[i]);
					}
				}
				return new Message(b, array);
			}
			catch (Exception ex)
			{
				Debug.Log(ex.StackTrace.ToString());
			}
			return null;
		}
	}

	protected static Session_ME instance = new Session_ME();

	private static NetworkStream dataStream;

	private static BinaryReader dis;

	private static BinaryWriter dos;

	public static IMessageHandler messageHandler;

	public static bool isMainSession = true;

	private static TcpClient sc;

	public static volatile bool connected;

	public static volatile bool connecting;

	private static Sender sender = new Sender();

	public static Thread initThread;

	public static Thread collectorThread;

	public static Thread sendThread;

	public static int sendByteCount;

	public static int recvByteCount;

	private static volatile bool getKeyComplete;

	public static sbyte[] key = null;

	private static sbyte curR;

	private static sbyte curW;

	private static int timeConnected;

	private long lastTimeConn;

	public static string strRecvByteCount = string.Empty;

	public static bool isCancel;

	private string host;

	private int port;

	private long timeWaitConnect;

	public static int count;

	public static MyVector recieveMsg = new MyVector();

	public Session_ME()
	{
		Debug.Log("init Session_ME");
	}

	public void clearSendingMessage()
	{
		lock (sender.sendingMessage) sender.sendingMessage.Clear();
	}

	public static bool readyForLogin()
	{
		return connected && !connecting && getKeyComplete;
	}

	public static void clearReceivedMessages()
	{
		lock (recieveMsg) recieveMsg.removeAllElements();
	}

	public static Session_ME gI()
	{
		if (instance == null)
		{
			instance = new Session_ME();
		}
		return instance;
	}

	public bool isConnected()
	{
		return connected && sc != null && dis != null;
	}

	public void setHandler(IMessageHandler msgHandler)
	{
		messageHandler = msgHandler;
	}

	public void connect(string host, int port)
	{
		if (isMainSession) CustomServerAddress.Resolve(ref host, ref port);
		lock (networkLock)
		{
			if (!connected && !connecting && mSystem.currentTimeMillis() >= timeWaitConnect)
			{
				timeWaitConnect = mSystem.currentTimeMillis() + 50;
				if (isMainSession)
				{
					ServerListScreen.testConnect = -1;
				}
				this.host = host;
				this.port = port;
				getKeyComplete = false;
				close();
				connecting = true;
				Debug.Log("connecting...!");
				Debug.Log("host: " + host);
				Debug.Log("port: " + port);
				int generation = networkGeneration;
				initThread = new Thread(() => NetworkInit(generation));
				initThread.Start();
			}
		}
	}

	private void NetworkInit(int generation)
	{
		isCancel = false;
		Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
		try
		{
			doConnect(host, port, generation);
		}
		catch (Exception)
		{
			lock (networkLock)
			{
				if (generation == networkGeneration && messageHandler != null)
				{
					close();
					messageHandler.onConnectionFail(isMainSession);
				}
			}
		}
	}

	public void doConnect(string host, int port)
	{
		doConnect(host, port, networkGeneration);
	}

	private void doConnect(string host, int port, int generation)
	{
		TcpClient socket = connectSocket(host, port, isMainSession);
		lock (networkLock)
		{
			if (generation != networkGeneration)
			{
				socket.Close();
				return;
			}
			sc = socket;
			sc.SendTimeout = 5000;
			Debug.Log("Connected to " + sc.Client.RemoteEndPoint);
			dataStream = sc.GetStream();
			dis = new BinaryReader(dataStream, new UTF8Encoding());
			dos = new BinaryWriter(dataStream, new UTF8Encoding());
			connected = true;
			sendThread = new Thread(() => sender.run(generation));
			sendThread.Start();
			MessageCollector @object = new MessageCollector();
			Cout.LogError("new -----");
			collectorThread = new Thread(@object.run);
			collectorThread.Start();
			timeConnected = currentTimeMillis();
			connecting = false;
			doSendMessage(new Message(-27));
			// Publish the connect event before the collector can publish handshake readiness.
			messageHandler.onConnectOK(isMainSession);
		}
	}

	private static TcpClient connectSocket(string host, int port, bool allowFallback)
	{
		TcpClient client = new TcpClient();
		try
		{
			IAsyncResult pending = client.BeginConnect(host, port, null, null);
			using (WaitHandle waitHandle = pending.AsyncWaitHandle)
			{
				if (!waitHandle.WaitOne(3000))
				{
					throw new TimeoutException("TCP connection timed out.");
				}
				client.EndConnect(pending);
			}
			return client;
		}
		catch (Exception ex) when (ex is SocketException || ex is TimeoutException)
		{
			client.Close();
			IPAddress address;
			bool isLocalhost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
				|| (IPAddress.TryParse(host, out address) && IPAddress.IsLoopback(address));
			if (allowFallback && !isLocalhost)
			{
				return connectSocket("127.0.0.1", port, false);
			}
			throw;
		}
		catch
		{
			client.Close();
			throw;
		}
	}

	public void sendMessage(Message message)
	{
		count++;
		Res.outz("SEND MSG: " + message.command);
		sender.AddMessage(message);
	}

	private static void doSendMessage(Message m)
	{
		sbyte[] data = m.getData();
		try
		{
			if (getKeyComplete)
			{
				sbyte value = writeKey(m.command);
				dos.Write(value);
			}
			else
			{
				dos.Write(m.command);
			}
			if (data != null)
			{
				int num = data.Length;
				if (getKeyComplete)
				{
					int num2 = writeKey((sbyte)(num >> 8));
					dos.Write((sbyte)num2);
					int num3 = writeKey((sbyte)(num & 0xFF));
					dos.Write((sbyte)num3);
				}
				else
				{
					dos.Write((ushort)num);
				}
				if (getKeyComplete)
				{
					for (int i = 0; i < data.Length; i++)
					{
						sbyte value2 = writeKey(data[i]);
						dos.Write(value2);
					}
				}
				sendByteCount += 5 + data.Length;
			}
			else
			{
				if (getKeyComplete)
				{
					int num4 = 0;
					int num5 = writeKey((sbyte)(num4 >> 8));
					dos.Write((sbyte)num5);
					int num6 = writeKey((sbyte)(num4 & 0xFF));
					dos.Write((sbyte)num6);
				}
				else
				{
					dos.Write((ushort)0);
				}
				sendByteCount += 5;
			}
			dos.Flush();
		}
		catch (Exception ex)
		{
			Debug.Log(ex.StackTrace);
			dos.Flush();
		}
	}

	public static sbyte readKey(sbyte b)
	{
		sbyte[] array = key;
		sbyte num = curR;
		curR = (sbyte)(num + 1);
		sbyte result = (sbyte)((array[num] & 0xFF) ^ (b & 0xFF));
		if (curR >= key.Length)
		{
			curR = (sbyte)(curR % (sbyte)key.Length);
		}
		return result;
	}

	public static sbyte writeKey(sbyte b)
	{
		sbyte[] array = key;
		sbyte num = curW;
		curW = (sbyte)(num + 1);
		sbyte result = (sbyte)((array[num] & 0xFF) ^ (b & 0xFF));
		if (curW >= key.Length)
		{
			curW = (sbyte)(curW % (sbyte)key.Length);
		}
		return result;
	}

	public static void onRecieveMsg(Message msg)
	{
		if (Thread.CurrentThread.Name == Main.mainThreadName)
		{
			messageHandler.onMessage(msg);
		}
		else
		{
			lock (recieveMsg) recieveMsg.addElement(msg);
		}
	}

	public static void update()
	{
		while (true)
		{
			Message message;
			lock (recieveMsg)
			{
				if (Controller.isStopReadMessage || recieveMsg.size() == 0) break;
				message = (Message)recieveMsg.elementAt(0);
				recieveMsg.removeElementAt(0);
			}
			if (message == null) break;
			messageHandler.onMessage(message);
		}
	}

	public void close()
	{
		cleanNetwork();
	}

	private static void cleanNetwork()
	{
		lock (networkLock)
		{
			networkGeneration++;
			getKeyComplete = false;
			key = null;
			curR = 0;
			curW = 0;
			try
			{
				connected = false;
				connecting = false;
				if (sc != null)
				{
					sc.Close();
					sc = null;
				}
				if (dataStream != null)
				{
					dataStream.Close();
					dataStream = null;
				}
				if (dos != null)
				{
					dos.Close();
					dos = null;
				}
				if (dis != null)
				{
					dis.Close();
					dis = null;
				}
				if (Thread.CurrentThread.Name == Main.mainThreadName)
				{
					abortNetworkThread(sendThread);
					sendThread = null;
					abortNetworkThread(initThread);
					initThread = null;
					abortNetworkThread(collectorThread);
					collectorThread = null;
				}
				if (isMainSession)
				{
					ServerListScreen.testConnect = 0;
				}
			}
			catch (Exception)
			{
			}
		}
	}

	private static void abortNetworkThread(Thread thread)
	{
		if (thread == null || !thread.IsAlive) return;
		try { thread.Abort(); }
		catch (ThreadStateException) { }
	}

	public static int currentTimeMillis()
	{
		return Environment.TickCount;
	}

	public static byte convertSbyteToByte(sbyte var)
	{
		if (var > 0)
		{
			return (byte)var;
		}
		return (byte)(var + 256);
	}

	public static byte[] convertSbyteToByte(sbyte[] var)
	{
		byte[] array = new byte[var.Length];
		for (int i = 0; i < var.Length; i++)
		{
			if (var[i] > 0)
			{
				array[i] = (byte)var[i];
			}
			else
			{
				array[i] = (byte)(var[i] + 256);
			}
		}
		return array;
	}

	public bool isCompareIPConnect()
	{
		return true;
	}
}
