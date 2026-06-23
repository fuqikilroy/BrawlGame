using System.Collections;
using System.Collections.Generic;

using System.Net.Sockets;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


using UnityEngine;

public static class NetworkManager {
	//定义套接字
	static Socket socket;
	//接收缓冲区
	static ByteArray readBuff;
	//写入队列
	static Queue<ByteArray> writeQueue;
    static AutoResetEvent writeQueueReadyEvent = new AutoResetEvent(false);
	//是否正在连接
	static bool isConnecting = false;
	//是否正在关闭
	static bool isClosing = false;
	//消息列表
	static List<MsgBase> msgList = new List<MsgBase>();
	//消息列表长度
	static int msgCount = 0;
	//每一次Update处理的消息量
	readonly static int MAX_MESSAGE_FIRE = 10;
	//是否启用心跳
	public static bool isUsePing = true;
	//心跳间隔时间
	public static int pingInterval = 30;
	//上一次发送PING的时间
	static float lastPingTime = 0;
	//上一次收到PONG的时间
	static float lastPongTime = 0;

	//事件
	public enum NetworkEvent
	{
		ConnectSucc = 1,
		ConnectFail = 2,
		Close = 3,
	}
    //事件委托类型    /***C#委托***/
    //定义一个函数长什么样子
    //所有和这个函数长一样的函数，都可以赋值给他
	//（观察者模式）
    //可以通过+=把多个方法添加到这个委托中，形成一个方法的执行链，执行委托的时候，按照添加方法的顺序，依次去执行方法，
    public delegate void EventListener(String err);
	//事件监听列表
	private static Dictionary<NetworkEvent, EventListener> eventListeners = new Dictionary<NetworkEvent, EventListener>();
	//添加事件监听
	public static void AddEventListener(NetworkEvent networkEvent, EventListener listener){
		//添加事件
		if (eventListeners.ContainsKey(networkEvent)){
			//添加一个委托
			eventListeners[networkEvent] += listener;
		}
		//新增事件
		else{
			eventListeners[networkEvent] = listener;
		}
	}
	//删除事件监听
	public static void RemoveEventListener(NetworkEvent networkEvent, EventListener listener){
		if (eventListeners.ContainsKey(networkEvent)){
			eventListeners[networkEvent] -= listener;
			if (eventListeners[networkEvent] == null) {
				eventListeners.Remove(networkEvent);
			}
		}
	}
	//分发事件
	private static void FireEvent(NetworkEvent networkEvent, String err){
		if(eventListeners.ContainsKey(networkEvent)){
			//调用所有的赋值给委托的函数
			eventListeners[networkEvent](err);
		}
	}


	//消息委托类型
	public delegate void MsgListener(MsgBase msgBase);
	//消息监听列表
	private static Dictionary<string, MsgListener> msgListeners = new Dictionary<string, MsgListener>();
	//添加消息监听
	public static void AddMsgListener(string msgName, MsgListener listener){
		//添加
		if (msgListeners.ContainsKey(msgName)){
			msgListeners[msgName] += listener;
		}
		//新增
		else{
			msgListeners[msgName] = listener;
		}
	}
	//删除消息监听
	public static void RemoveMsgListener(string msgName, MsgListener listener){
		if (msgListeners.ContainsKey(msgName)){
			msgListeners[msgName] -= listener;
			if (msgListeners[msgName] == null) {
				msgListeners.Remove(msgName);
			}
		}
	}
	//分发消息
	private static void FireMsg(string msgName, MsgBase msgBase){
		if(msgListeners.ContainsKey(msgName)){
			msgListeners[msgName](msgBase);
		}
	}


	//连接
	public static async void Connect(string ip, int port)
	{
		//状态判断
		if(socket!=null && socket.Connected){
			Debug.Log("Connect fail, already connected!");
			return;
		}
		if(isConnecting){
			Debug.Log("Connect fail, isConnecting");
			return;
		}
		//初始化成员
		InitState();
		//参数设置
		socket.NoDelay = true;
		//Connect
		isConnecting = true;
        
		//socket.BeginConnect(ip, port, ConnectCallback, socket);

        try {
			await socket.ConnectAsync(ip, port);

			//连接成功后，之后的代码开始执行
            Debug.Log("Socket Connect Succ ");
			FireEvent(NetworkEvent.ConnectSucc,"");
			isConnecting = false;

			OnSend();
			OnReceive();
		}
		catch (System.Exception ex) {
			Debug.Log("Socket Connect fail " + ex.ToString());
			FireEvent(NetworkEvent.ConnectFail, ex.ToString());
			isConnecting = false;
		}
	}

	//初始化状态
	private static void InitState(){
		//Socket
		socket = new Socket(AddressFamily.InterNetwork,
			SocketType.Stream, ProtocolType.Tcp);
		//接收缓冲区
		readBuff = new ByteArray();
		//写入队列
		writeQueue = new Queue<ByteArray>();
		//是否正在连接
		isConnecting = false;
		//是否正在关闭
		isClosing = false;
		//消息列表
		msgList = new List<MsgBase>();
		//消息列表长度
		msgCount = 0;
		//上一次发送PING的时间
		lastPingTime = Time.time;
		//上一次收到PONG的时间
		lastPongTime = Time.time;
		//监听PONG协议
		if(!msgListeners.ContainsKey("MsgPong")){
			AddMsgListener("MsgPong", OnMsgPong);
		}
	}

    static async void OnReceive() {
		while (true) {
			try {
				int received = await socket
								.ReceiveAsync(new System.ArraySegment<byte>(readBuff.bytes, readBuff.writeIdx, readBuff.remain), 
								SocketFlags.None);
			
				if (received == 0) {
					continue;
				}

				Debug.Log("Recived " + received);
				readBuff.writeIdx += received;
				OnReceiveData();

				//继续接收数据
				if(readBuff.remain < 8){
					readBuff.MoveBytes();
					readBuff.ReSize(readBuff.length*2);
				}
			}
			catch (System.Exception ex){
				Debug.Log("Socket recv fail " + ex.ToString());
				FireEvent(NetworkEvent.Close, "");
				break;
			}
			
		}
	}

    //数据处理
	public static void OnReceiveData(){
		//消息长度
		if(readBuff.length <= 2) {
			return;
		}
		//获取消息体长度
		int readIdx = readBuff.readIdx;
		byte[] bytes =readBuff.bytes; 
		Int16 bodyLength = (Int16)((bytes[readIdx+1] << 8 )| bytes[readIdx]);
		if(readBuff.length < bodyLength)
			return;
		readBuff.readIdx+=2; 
		//解析协议名
		int nameCount = 0;
		string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIdx, out nameCount);
		if(protoName == ""){
			Debug.Log("OnReceiveData MsgBase.DecodeName fail");
			return;
		}
		readBuff.readIdx += nameCount;
		//解析协议体
		int bodyCount = bodyLength - nameCount;
		MsgBase msgBase = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIdx, bodyCount);
		readBuff.readIdx += bodyCount;
		readBuff.CheckAndMoveBytes();
		//添加到消息队列
		lock(msgList){
			msgList.Add(msgBase);
			msgCount++;
		}
		//继续读取消息
		if(readBuff.length > 2){
			OnReceiveData();
		}
	}


	//关闭连接
	public static void Close(){
		//状态判断
		if(socket==null || !socket.Connected){
			return;
		}
		if(isConnecting){
			return;
		}
		//还有数据在发送
		if(writeQueue.Count > 0){
			isClosing = true;
		} 
		//没有数据在发送
		else{
			socket.Close();
			FireEvent(NetworkEvent.Close, "");
		} 
	} 

	//发送数据
	public static void Send(MsgBase msg) {   //MsgBase msg多态，可以用基类表示所有的子类
		//状态判断
		if(socket==null || !socket.Connected){
			return;
		}
		if(isConnecting){
			return;
		}
		if(isClosing){
			return;
		} 
		//数据编码
		byte[] nameBytes = MsgBase.EncodeName(msg);
		byte[] bodyBytes = MsgBase.Encode(msg);
		int len = nameBytes.Length + bodyBytes.Length;
		byte[] sendBytes = new byte[2+len];
		//组装长度
		sendBytes[0] = (byte)(len%256);
		sendBytes[1] = (byte)(len/256);
		//组装名字
		Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
		//组装消息体
		Array.Copy(bodyBytes, 0, sendBytes, 2+nameBytes.Length, bodyBytes.Length);
		//写入队列
		ByteArray ba = new ByteArray(sendBytes);
		int count = 0;	//writeQueue的长度
		lock(writeQueue){
			writeQueue.Enqueue(ba);
			count = writeQueue.Count;
		}
		//send
		if(writeQueue.Count == 1) {
			// 发送队列中有数据
			writeQueueReadyEvent.Set();  
		}
	}

    private static async void OnSend() {
        while (true) {

            if(socket == null || !socket.Connected){
			    break;
		    }

            ByteArray ba = null;

			if (writeQueue.Count == 0) {

                if(isClosing) {
                    socket.Close();
                    return;
                } 
				Debug.Log("Wait for data to send.");
				await Task.Run(() =>  
				{  
					writeQueueReadyEvent.WaitOne(); // 这将阻塞后台线程，但不会阻塞当前 async 方法的调用线程  
				});  
			}

			ba = writeQueue.First();

			Debug.Log(ba.Debug());

            try {
                while (ba != null && ba.length > 0) {

                    ba.readIdx += await socket
                                    .SendAsync(new System.ArraySegment<byte>(ba.bytes, 
                                                ba.readIdx, ba.length), SocketFlags.None);
		
			    } 
				lock(writeQueue){
					writeQueue.Dequeue();
				}   
            }
            catch (System.Exception ex) {
                Debug.Log("Socket Send fail " + ex.ToString());
				FireEvent(NetworkEvent.Close, "");
				break;
            }	
		}
    }

	//Update
	public static void Update(){
		MsgUpdate();
		PingUpdate();

    }

	//更新消息
	public static void MsgUpdate(){
		//初步判断，提升效率
		if(msgCount == 0){
			return;
		}
		//重复处理消息
		for(int i = 0; i< MAX_MESSAGE_FIRE; i++){
			//获取第一条消息
			MsgBase msgBase = null;
			lock(msgList){
				if(msgList.Count > 0){
					msgBase = msgList[0];
					msgList.RemoveAt(0);
					msgCount--;
				}
			}
			//分发消息
			if(msgBase != null){
				FireMsg(msgBase.protoName, msgBase);
			}
			//没有消息了
			else{
				break;
			}
		}
	}

	//发送PING协议
	private static void PingUpdate(){
		//是否启用
		if(!isUsePing){
			return;
		}
		//发送PING
		if(Time.time - lastPingTime > pingInterval){
			MsgPing msgPing = new MsgPing();
			Send(msgPing);
			lastPingTime = Time.time;
		}
		//检测PONG时间
		if(Time.time - lastPongTime > pingInterval*4){
            Close();
		}
	}

	//监听PONG协议
	private static void OnMsgPong(MsgBase msgBase){
		lastPongTime = Time.time;
	}
}
