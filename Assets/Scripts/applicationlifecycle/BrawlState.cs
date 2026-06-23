using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.InputSystem;
//using UnityEngine.InputSystem.Controls;
//using static UnityEditor.Experimental.GraphView.GraphView;
//using UnityEditor.PackageManager;


public class BrawlState : MonoBehaviour
{

    //人物模型预设
    public GameObject[] characters;

    public Text clientNameText;
    //人物列表
    private Player curPlayer;
    private Dictionary<string, Player> otherPlayers = new Dictionary<string, Player>();
    //Dropdown组件
    public Dropdown dropdown; // 将Dropdown组件绑定到此字段

    void Start()
    {

        //网络模块
        NetworkManager.AddMsgListener("MsgEnter", OnMsgEnter);
        NetworkManager.AddMsgListener("MsgList", OnMsgList);
        NetworkManager.AddMsgListener("MsgPlayerChange", OnMsgPlayerChange);
        NetworkManager.AddMsgListener("MsgMove", OnMsgMove);
        NetworkManager.AddMsgListener("MsgLeave", OnMsgLeave);
        NetworkManager.AddMsgListener("MsgAttack", OnMsgAttack);
        //NetworkManager.AddMsgListener("MsgDie", OnDie);

        PlayerData curPlayerData = GameObject.FindWithTag("ApplicationController").GetComponent<ApplicationController>().curPlayer;
        clientNameText.text = curPlayerData.clientId;

        //添加一个角色
        GameObject obj = (GameObject)Instantiate(characters[curPlayerData.characterIndex]);

        obj.name = "Player";

        curPlayer = obj.GetComponent<Player>();
        curPlayer.playerData = curPlayerData;

        obj.transform.position = new Vector3(curPlayerData.posX, curPlayerData.posY, curPlayerData.posZ);
        obj.transform.eulerAngles = new Vector3(0, curPlayerData.eulY, 0);


        Debug.Log("CliendId: " + curPlayerData.clientId);

        MsgEnter msgEnger = new MsgEnter();
        NetworkManager.Send(msgEnger);
        NetworkManager.Send(new MsgList());
    }

    void OnDestroy()
    {
        //网络模块
        NetworkManager.RemoveMsgListener("MsgEnter", OnMsgEnter);
        NetworkManager.RemoveMsgListener("MsgList", OnMsgList);
        NetworkManager.AddMsgListener("MsgPlayerChange", OnMsgPlayerChange);
        NetworkManager.RemoveMsgListener("MsgMove", OnMsgMove);
        NetworkManager.RemoveMsgListener("MsgLeave", OnMsgLeave);
        NetworkManager.RemoveMsgListener("MsgAttack", OnMsgAttack);
        // NetworkManager.RemoveMsgListener("MsgDie", OnDie);
    }

    void OnMsgEnter(MsgBase msgBase)
    {
        MsgEnter msg = (MsgEnter)msgBase;
        Debug.Log("OnMsgEnter " + msg);
        PlayerData enterPlayer = msg.playerData;

        if (enterPlayer.clientId == curPlayer.playerData.clientId)
        {
            return;
        }

        //添加一个角色
        GameObject obj = (GameObject)Instantiate(characters[enterPlayer.characterIndex]);
        obj.name = "otherPlayer";
        obj.transform.position = new Vector3(enterPlayer.posX, enterPlayer.posY, enterPlayer.posZ);
        obj.transform.eulerAngles = new Vector3(0, enterPlayer.eulY, 0);
        Player otherPlayer = obj.GetComponent<Player>();
        otherPlayer.playerData = enterPlayer;

        otherPlayers.Add(enterPlayer.clientId, otherPlayer);
    }

    void OnMsgList(MsgBase msgBase)
    {
        MsgList msg = (MsgList)msgBase;
        Debug.Log("OnMsgList " + msg);

        PlayerData[] players = msg.players;

        foreach (PlayerData player in players)
        {
            if (curPlayer.playerData.clientId == player.clientId)
            {
                continue;
            }

            //添加一个角色
            GameObject obj = (GameObject)Instantiate(characters[player.characterIndex]);
            obj.name = "otherPlayer";
            obj.transform.position = new Vector3(player.posX, player.posY, player.posZ);
            obj.transform.eulerAngles = new Vector3(0, player.eulY, 0);
            Player otherPlayer = obj.GetComponent<Player>();
            otherPlayer.playerData = player;
            otherPlayers.Add(player.clientId, otherPlayer);
        }
    }



    void OnMsgPlayerChange(MsgBase msgBase)
    {
        MsgPlayerChange msg = (MsgPlayerChange)msgBase;
        Debug.Log("OnMsgPlayerChange " + msg);

        //通过msg的clientId拿到对应的角色
        if (curPlayer.playerData.clientId == msg.clientId)
        {
            return;
        }

        Player changePlayer = null;

        if (otherPlayers.TryGetValue(msg.clientId, out changePlayer))
        {
            //Debug.Log(changePlayer.playerData.clientId + " Player changed");

            PlayerData playerData = msg.playerData;
            playerData.characterIndex = msg.characterIndex;         
            //添加一个角色
            GameObject obj = (GameObject)Instantiate(characters[playerData.characterIndex]);
            obj.name = "otherPlayer";

            Debug.Log(obj.name);

            obj.transform.position = new Vector3(playerData.posX, playerData.posY, playerData.posZ);
            obj.transform.eulerAngles = new Vector3(0, playerData.eulY, 0);
            //Debug.Log("新的坐标：" + changePlayer.playerData.posX + "," + changePlayer.playerData.posY + "," + changePlayer.playerData.posZ);

            //删除原来的角色
            Destroy(changePlayer.gameObject);

            changePlayer = obj.GetComponent<Player>();
            changePlayer.playerData = msg.playerData;
            changePlayer.playerData.characterIndex= msg.characterIndex;

            otherPlayers[playerData.clientId] = changePlayer;

        }


    }


    void OnMsgMove(MsgBase msgBase)
    {
        MsgMove msg = (MsgMove)msgBase;
        Debug.Log("OnMsgMove " + msg);

        Player movePlayer = null;

        if (otherPlayers.TryGetValue(msg.clientId, out movePlayer))
        {
            Debug.Log(msg.clientId + " want to move to " + msg.posX + "," + msg.posY + "," + msg.posZ);

            movePlayer.playerData.posX = msg.posX;
            movePlayer.playerData.posY = msg.posY;
            movePlayer.playerData.posZ = msg.posZ;
            movePlayer.MoveTo(new Vector3(msg.posX, msg.posY, msg.posZ));

        }
        if (!otherPlayers.ContainsKey(msg.clientId))
        {
            return;
        }

    }

    void OnMsgAttack(MsgBase msgBase)
    {
        MsgAttack msg = (MsgAttack)msgBase;
        Debug.Log("OnMsgAttack " + msg);

        Player attackPlayer = null;

        if (otherPlayers.TryGetValue(msg.clientId, out attackPlayer))
        {
            Debug.Log(msg.clientId + " want to attack to " + msg.eulY);

            attackPlayer.transform.eulerAngles = new Vector3(0, msg.eulY, 0);

            attackPlayer.Attack();
        }

        if (!otherPlayers.ContainsKey(msg.clientId))
        {
            return;
        }

    }

    void OnMsgLeave(MsgBase msgBase)
    {
        MsgLeave msg = (MsgLeave)msgBase;
        Debug.Log("OnMsgLeave " + msg);

        Player leavePlayer = null;

        if (otherPlayers.TryGetValue(msg.clientId, out leavePlayer))
        {
            Debug.Log(msg.clientId + " Leave ");

            Destroy(leavePlayer.gameObject);
            otherPlayers.Remove(msg.clientId);

        }

        if (!otherPlayers.ContainsKey(msg.clientId))
        {
            return;
        }

    }

    public void OnPlayerChange()
    {
        int characterIndex = dropdown.value;

        if (characterIndex == curPlayer.playerData.characterIndex)
        {
            return;
        }

        //Debug.Log("原来坐标：" + curPlayer.playerData.posX + "," + curPlayer.playerData.posY + "," + curPlayer.playerData.posZ);
        PlayerData curPlayerData = curPlayer.playerData;
        curPlayerData.characterIndex = characterIndex;
        //Debug.Log("当前坐标：" + curPlayerData.posX + "," + curPlayerData.posY + "," + curPlayerData.posZ);
        //Debug.Log("idddddddddddddddddddd：" + curPlayerData.clientId);


        //添加一个角色
        GameObject obj = (GameObject)Instantiate(characters[curPlayerData.characterIndex]);
        obj.name = "Player";


        obj.transform.position = new Vector3(curPlayerData.posX, curPlayerData.posY, curPlayerData.posZ);
        obj.transform.eulerAngles = new Vector3(0, curPlayerData.eulY, 0);
        Debug.Log("新的坐标：" + curPlayer.playerData.posX + "," + curPlayer.playerData.posY + "," + curPlayer.playerData.posZ);

        //删除原来的角色
        Destroy(curPlayer.gameObject);

        curPlayer = obj.GetComponent<Player>();
        curPlayer.playerData = curPlayerData;

        //将新的角色数据发送给服务器		
        MsgPlayerChange msgPlayerChange = new MsgPlayerChange();
        msgPlayerChange.clientId = curPlayer.playerData.clientId;
        msgPlayerChange.playerData = curPlayerData;
        msgPlayerChange.characterIndex = characterIndex;        
        //Debug.Log("dddata：" + curPlayerData.clientId);
        //Debug.Log("dddddddddata：" + msgPlayerChange.playerData.clientId);


        //服务器拿到处理后再转发给其他客户端
        NetworkManager.Send(msgPlayerChange);
    }

    void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            //判断鼠标是否在窗口内
            if (!new Rect(0, 0, Screen.width, Screen.height).Contains(Mouse.current.position.ReadValue()))
            {
                Debug.Log("Mouse is not in windows");
                return;
            }
            curPlayer.Attack();

            MsgAttack msgAttack = new MsgAttack();
            msgAttack.clientId = curPlayer.playerData.clientId;
            // 发送Attack
            NetworkManager.Send(msgAttack);
        }
    }



    void OnMove(InputValue value)
    {
        if (value.isPressed)
        {
            //判断鼠标是否在窗口内
            if (!new Rect(0, 0, Screen.width, Screen.height).Contains(Mouse.current.position.ReadValue()))
            {
                Debug.Log("Mouse is not in windows");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            Physics.Raycast(ray, out hit);
            if (hit.collider.tag == "Terrain")
            {
                Debug.Log("I want to move to " + hit.point);
                Debug.Log("原来坐标：" + curPlayer.playerData.posX + "," + curPlayer.playerData.posY + "," + curPlayer.playerData.posZ);
                curPlayer.MoveTo(hit.point);
                curPlayer.playerData.posX = hit.point.x;
                curPlayer.playerData.posY = hit.point.y;
                curPlayer.playerData.posZ = hit.point.z;
                Debug.Log("移动后的坐标：" + curPlayer.playerData.posX + "," + curPlayer.playerData.posY + "," + curPlayer.playerData.posZ);


                MsgMove msgMove = new MsgMove();
                msgMove.clientId = curPlayer.playerData.clientId;
                msgMove.posX = hit.point.x;
                msgMove.posY = hit.point.y;
                msgMove.posZ = hit.point.z;

                NetworkManager.Send(msgMove);
            }
        }
    }


}
