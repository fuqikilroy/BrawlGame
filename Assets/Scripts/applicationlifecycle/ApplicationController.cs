using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationController : MonoBehaviour {
	public static ApplicationController Instance {get; private set;}
	public PlayerData curPlayer {get; set;}

	void Awake () {
		if (Instance == null) {
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else {
			Destroy(gameObject);
		}
	}

	void Update(){
		NetworkManager.Update();
	}
}
