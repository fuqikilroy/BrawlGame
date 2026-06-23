using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RegisterLogin : MonoBehaviour {

	public InputField hostIP;
    public InputField hostPort;
	public InputField userName;
    public InputField password;
    public Button connectButton;
    public Button loginButton;
	public Button registerButton;


	void Start() {
		NetworkManager.AddEventListener(NetworkManager.NetworkEvent.ConnectSucc, OnConnected);
		NetworkManager.AddEventListener(NetworkManager.NetworkEvent.ConnectFail, OnConnectFailed);
		NetworkManager.AddEventListener(NetworkManager.NetworkEvent.Close, OnClosed);

		NetworkManager.AddMsgListener("MsgRegister", OnMsgRegister);
		NetworkManager.AddMsgListener("MsgLogin", OnMsgLogin);

		
		connectButton.interactable = true;
		loginButton.interactable = false;
        registerButton.interactable = false;

		hostIP.text = "127.0.0.1";
		hostPort.text = "8888";
	}

	void OnDestroy() {

		NetworkManager.RemoveEventListener(NetworkManager.NetworkEvent.ConnectSucc, OnConnected);
		NetworkManager.RemoveEventListener(NetworkManager.NetworkEvent.ConnectFail, OnConnectFailed);
		NetworkManager.RemoveEventListener(NetworkManager.NetworkEvent.Close, OnClosed);

		NetworkManager.RemoveMsgListener("MsgRegister", OnMsgRegister);
		NetworkManager.RemoveMsgListener("MsgLogin", OnMsgLogin);
	}

	void OnConnected(string err) {
		connectButton.interactable = false;
		loginButton.interactable = true;
		registerButton.interactable = true;
	}

	void OnConnectFailed(string err) {
		connectButton.interactable = true;
	}

	void OnClosed(string err) {
		connectButton.interactable = true;
		loginButton.interactable = false;
        registerButton.interactable = false;
	}

	public async void OnConnectClick()
	{
	
		connectButton.interactable = false;
		
		if ("" == hostIP.text || "" == hostPort.text) {
			Debug.LogError("There is at least one inputfield empty.");
			return;
		}
		string ip = hostIP.text;
		int port = int.Parse(hostPort.text);

		NetworkManager.Connect(ip, port);
	}

	public async void OnRegisterClick()
	{
		string name = userName.text;
		string pw = password.text;
		

		if (name == "" || pw == "") {
			Debug.LogError("There is at least one inputfield empty.");
			return;
		}

		MsgRegister msgRegister = new MsgRegister();
		msgRegister.id = name;
		msgRegister.pw = pw;

		NetworkManager.Send(msgRegister);
	}

	public async void OnLoginClick()
	{
		string name = userName.text;
		string pw = password.text;
		

		if (name == "" || pw == "") {
			Debug.LogError("There is at least one inputfield empty.");
			return;
		}

		MsgLogin msgLogin = new MsgLogin();
		msgLogin.id = name;
		msgLogin.pw = pw;

		NetworkManager.Send(msgLogin);
	}

	void OnMsgRegister (MsgBase msgBase) {
		MsgRegister msg = (MsgRegister)msgBase;
		Debug.Log("OnRegister " + msg);
		if (msg.result == 0) {
			// 注册成功
			Debug.Log("Succeed to register!");
		}
		else {
			Debug.LogError("Fail to register!");
		}
	}

	void OnMsgLogin(MsgBase msgBase) {
		MsgLogin msg = (MsgLogin)msgBase;
		if (msg.result == 0) {
			// 登录成功
			Debug.Log("Succeed to login!");

			Debug.Log("player clientId " + msg.playerData.clientId);

			GameObject.Find("ApplicationController").GetComponent<ApplicationController>().curPlayer = msg.playerData;

			// 加载场景
			SceneManager.LoadScene("BrawlGame");
		}
		else {
			Debug.LogError("Fail to login!");
		}
	}



}
