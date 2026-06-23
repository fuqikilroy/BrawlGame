
//加入
public class MsgEnter:MsgBase {
	public MsgEnter() {protoName = "MsgEnter";}
	//客户端发
	public string clientId = "";

	//服务器回
	public PlayerData playerData;

}


//列表
public class MsgList:MsgBase {
	public MsgList() {protoName = "MsgList";}

	//服务端回
	public PlayerData[] players;
}

//角色切换
public class MsgPlayerChange : MsgBase {
	public MsgPlayerChange() {protoName = "MsgPlayerChange"; }

    //客户端发
    public string clientId = "";
    public int characterIndex = 0;
    //服务端回
    public PlayerData playerData;
}

//移动
public class MsgMove:MsgBase {
	public MsgMove() {protoName = "MsgMove";}
	//客户端发
	public string clientId = "";
	public float posX = 0.0F;
	public float posY = 0.0F;
	public float posZ = 0.0F;
}

//攻击
public class MsgAttack:MsgBase {
	public MsgAttack() {protoName = "MsgAttack";}
	//客户端发
	public string clientId = "";
	public float eulY = 0.0F;
}
////击中
//public class MsgHit:MsgBase {
//	public MsgHit() {protoName = "MsgHit"; }
//	//客户端发
//	public string attackClientId = "";
//	public string hitClientId = "";
//}

//退出
public class MsgLeave:MsgBase {
	public MsgLeave() {protoName = "MsgLeave";}
	//服务器发
	public string clientId = "";
}
