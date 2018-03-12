using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using Photon;
namespace M3PUN {
	public class ConnectionController : PunBehaviour {

		public Action M3OnConnectedToMaster;
		public Action<DisconnectCause> M3OnFailedToConnectToPhoton;
		public Action M3OnJoinedLobby;
		public Action M3OnLeftLobby;
		public Action<object[]> M3OnPhotonJoinRoomFailed;
		public Action<object[]> M3OnPhotonRandomJoinFailed;
		public Action M3OnJoinedRoom;
		public Action M3OnCreatedRoom;
		public Action M3OnLeftRoom;
		public Action<object> M3OnPhotonCreateRoomFailed;
		public Action M3OnDisconnectedFromPhoton;
		public Action<DisconnectCause> M3OnConnectionFail;
		public Action<PhotonPlayer> M3OnPhotonPlayerDisconnected;
		public Action<PhotonPlayer> M3OnPhotonPlayerConnected;
		public Action M3OnPhotonMaxCccuReached;
		public PhotonNetwork.EventCallback OnEventCallHandler;
		public bool offlineMode { get { return PhotonNetwork.offlineMode; } }
		public bool connected { get { return PhotonNetwork.connected; } }
		public bool connectedAndReady { get { return PhotonNetwork.connectedAndReady; } }
		public bool connecting { get { return PhotonNetwork.connecting; } }
		public bool insideLobby { get { return PhotonNetwork.insideLobby; } }
		public bool inRoom { get { return PhotonNetwork.inRoom; } }
		public bool isMasterClient { get { return PhotonNetwork.isMasterClient; }}
		public bool isMessageQueueRunning { set { PhotonNetwork.isMessageQueueRunning = value; } get { return PhotonNetwork.isMessageQueueRunning;}}
		public Room room { get { return PhotonNetwork.room; } }
		public ConnectionState connectionState { get { return PhotonNetwork.connectionState; }}
		public ClientState connectionStateDetailed { get { return PhotonNetwork.connectionStateDetailed; }}
		public bool ConnectedAndOnline { get { return (PhotonNetwork.connected) && !PhotonNetwork.offlineMode; }}
		private static ConnectionController _instance;
		private static object _lock = new object();
		private static bool applicationIsQuitting = false;
		public static ConnectionController Instance
		{
			get
			{
				if (applicationIsQuitting) {
					Debug.LogWarning("[Singleton] Instance '"+ typeof(ConnectionController) +
						"' already destroyed on application quit." +
						" Won't create again - returning null.");
					return null;
				}

				lock(_lock)
				{
					if (_instance == null)
					{
						_instance = (ConnectionController) FindObjectOfType(typeof(ConnectionController));

						if ( FindObjectsOfType(typeof(ConnectionController)).Length > 1 )
						{
							Debug.LogError("[Singleton] Something went really wrong " +
								" - there should never be more than 1 singleton!" +
								" Reopening the scene might fix it.");
							return _instance;
						}

						if (_instance == null)
						{
							GameObject singleton = new GameObject();
							_instance = singleton.AddComponent<ConnectionController>();
							singleton.name = "(singleton) "+ typeof(ConnectionController).ToString();

							DontDestroyOnLoad(singleton);

							Debug.Log("[Singleton] An instance of " + typeof(ConnectionController) + 
								" is needed in the scene, so '" + singleton +
								"' was created with DontDestroyOnLoad.");
						} else {
							Debug.Log("[Singleton] Using instance already created: " +
								_instance.gameObject.name);
						}
					}

					return _instance;
				}
			}
		}

		public void Initialize(string UserId, string NickName)
		{
			#if CONFIG_LIVE
			PhotonNetwork.logLevel = PhotonLogLevel.ErrorsOnly;
			#else
			PhotonNetwork.logLevel = PhotonLogLevel.Informational;
			#endif

			PhotonNetwork.autoJoinLobby = false;
			PhotonNetwork.automaticallySyncScene = false;
			PhotonNetwork.BackgroundTimeout = 500f;
			
			PhotonNetwork.MaxResendsBeforeDisconnect = 10;
			PhotonNetwork.QuickResends = 3;
			PhotonNetwork.sendRate = 2;
			PhotonNetwork.sendRateOnSerialize = 2;
			AuthenticationValues auth = new AuthenticationValues ();

			auth.AuthType = CustomAuthenticationType.None;
			auth.AddAuthParameter ("UserId", UserId);
			auth.AddAuthParameter ("access_token", "offlien_token");	//this token isn't required as yet.
			
			PhotonNetwork.playerName = NickName;
			PhotonNetwork.AuthValues = auth;			
			M3Utils.Instance.GameRestarted += OnGameRestart;
			PhotonNetwork.OnEventCall += OnEventCall;

		}

		void OnGameRestart(object sender, EventArgs args) {
			if(ConnectedAndOnline) {
				PhotonNetwork.Disconnect();
			}
		}
			
		public void OnDestroy ()
		{
			PhotonNetwork.OnEventCall -= OnEventCall;
			M3Utils.Instance.GameRestarted -= OnGameRestart;
			applicationIsQuitting = true;
		}

		public void Connect(string GameVersion = M3Constants.PHOTON_GAME_VERSION)
		{
			Debug.Log ("Connect(), ConnectUsingSettings");
			if(PhotonNetwork.AuthValues == null) {
				Debug.LogError("Authentication Values Not Set");
			}
			
			PhotonNetwork.ConnectUsingSettings (GameVersion);
		}
		public void SwitchToOfflineMode() {
			PhotonNetwork.offlineMode = true;
		}

		public override void OnConnectedToMaster ()
		{
			base.OnConnectedToMaster ();
			Debug.Log("OnConnectedToMaster()");
			if (M3OnConnectedToMaster != null)
			{
				M3OnConnectedToMaster ();
			}
		}
		public override void OnFailedToConnectToPhoton (DisconnectCause cause)
		{
			base.OnFailedToConnectToPhoton (cause);
			Debug.Log("OnFailedToConnectToPhoton()");
			if (M3OnFailedToConnectToPhoton != null)
			{
				M3OnFailedToConnectToPhoton (cause);
			}
		}

		public void Disconnect()
		{
			if(PhotonNetwork.connected) {
				PhotonNetwork.Disconnect ();
			} else {
				OnDisconnectedFromPhoton();
			}

		}

		//Can be used to reconnect to the master server after a disconnect.
		//Connection callbacks will fire
		public bool Reconnect()
		{
			return PhotonNetwork.Reconnect ();
		}
		//When the client lost connection during gameplay, this method attempts to reconnect and rejoin the room.
		//Room callbacks will be fired
		public bool ReconnectAndRejoin()
		{
			return PhotonNetwork.ReconnectAndRejoin ();
		}
		public void SendOutgoingCommands() {
			PhotonNetwork.SendOutgoingCommands();
		}
		public void LoadLevelAsyncViaPhoton(string levelName) {
			PhotonNetwork.LoadLevel(levelName);
		}
		public TypedLobby GetTypedLobby() {
			return new TypedLobby(M3Constants.PHOTON_GAME_VERSION, LobbyType.SqlLobby);
		}
		public void JoinLobby(TypedLobby lobby=null)
		{
			lobby = lobby ?? GetTypedLobby();
			Debug.Log ("JoinLobby()");
			PhotonNetwork.JoinLobby (lobby);
		}
		public override void OnJoinedLobby ()
		{
			base.OnJoinedLobby ();
			Debug.Log ("OnJoinedLobby()");
			if (M3OnJoinedLobby != null)
			{
				M3OnJoinedLobby ();
			}
		}

		public void OnEventCall(byte eventcode, object content, int senderid) {
			if(OnEventCallHandler!= null) {
				OnEventCallHandler(eventcode, content, senderid);
			}
		}

		public void FetchServerTimeStamp() {
			PhotonNetwork.FetchServerTimestamp();
		}
		public void LeaveLobby()
		{
			PhotonNetwork.LeaveLobby ();
		}
		public override void OnLeftLobby ()
		{
			base.OnLeftLobby ();
			Debug.Log ("OnLeftLobby()");
			if (M3OnLeftLobby != null)
			{
				M3OnLeftLobby ();
			}
		}


		public bool JoinRoom(string pRoomName)
		{
			return PhotonNetwork.JoinRoom (pRoomName);
		}

		public bool KickPlayer(PhotonPlayer player) {
			return PhotonNetwork.CloseConnection(player);
		}

		public bool TryToJoinRoomWithSQLQuery(string sqlLobbyFilter) {
			return PhotonNetwork.JoinRandomRoom (null, (byte) Utils.CTECH_PUN_MAX_PLAYERS, MatchmakingMode.FillRoom, GetTypedLobby(), sqlLobbyFilter, null);
		}
		public void JoinOrCreateRoom(string pRoomName, RoomOptions pRoomOptions, TypedLobby pTypedLobby=null)
		{
			pTypedLobby = pTypedLobby ?? GetTypedLobby();
			PhotonNetwork.JoinOrCreateRoom(pRoomName,pRoomOptions,pTypedLobby);
		}

		public override void OnJoinedRoom ()
		{
			base.OnJoinedRoom ();
			Debug.Log("OnJoinedRoomSuccess()");
			if (M3OnJoinedRoom != null)
			{
				M3OnJoinedRoom ();
			}
		}
		public override void OnPhotonJoinRoomFailed (object[] codeAndMsg)
		{
			base.OnPhotonJoinRoomFailed (codeAndMsg);
			Debug.Log("OnPhotonJoinRoomFailed()");
			if (M3OnPhotonJoinRoomFailed != null)
			{
				M3OnPhotonJoinRoomFailed (codeAndMsg);
			}
		}

		public override void OnPhotonRandomJoinFailed (object[] codeAndMsg)
		{
			base.OnPhotonRandomJoinFailed (codeAndMsg);
			Debug.Log("OnPhotonJoinRoomFailed()");
			if (M3OnPhotonRandomJoinFailed != null)
			{
				M3OnPhotonRandomJoinFailed (codeAndMsg);
			}
		}


		public void CreateRoom(string roomName, RoomOptions roomOptions, TypedLobby lobby)
		{
			PhotonNetwork.CreateRoom (roomName, roomOptions, lobby);		
		}
		public override void OnCreatedRoom ()
		{
			base.OnCreatedRoom ();
			Debug.Log("OnCreatedRoom()");
			if (M3OnCreatedRoom != null)
			{
				M3OnCreatedRoom ();
			}
		}
		public override void OnPhotonCreateRoomFailed (object[] codeAndMsg)
		{
			base.OnPhotonCreateRoomFailed (codeAndMsg);
			if (M3OnPhotonCreateRoomFailed != null)
			{
				M3OnPhotonCreateRoomFailed (codeAndMsg);
			}
		}

		public void LeaveRoom()
		{
			if(PhotonNetwork.inRoom) {
				PhotonNetwork.LeaveRoom ();
			} else {
				OnLeftRoom();
			}
		}
		public override void OnLeftRoom ()
		{
			base.OnLeftRoom ();
			Debug.Log("OnLeftRoom()");
			if (M3OnLeftRoom != null)
			{
				M3OnLeftRoom ();
			}
		}


		public override void OnPhotonPlayerConnected (PhotonPlayer newPlayer)
		{
			base.OnPhotonPlayerConnected (newPlayer);
			Debug.Log ("OnPhotonPlayerConnected()");
			if (M3OnPhotonPlayerConnected != null)
			{
				M3OnPhotonPlayerConnected (newPlayer);
			}
		}
		public override void OnPhotonPlayerDisconnected (PhotonPlayer otherPlayer)
		{
			base.OnPhotonPlayerDisconnected (otherPlayer);
			Debug.Log ("OnPhotonPlayerDisconnected()");
			if (M3OnPhotonPlayerDisconnected != null)
			{
				M3OnPhotonPlayerDisconnected (otherPlayer);
			}
		}


		public override void OnConnectionFail (DisconnectCause cause)
		{
			Debug.Log ("=======================================");
			base.OnConnectionFail (cause);
			Debug.Log("OnConnectionFail()" + cause.ToString());
			Debug.Log ("=======================================");
			if (M3OnConnectionFail != null)
			{
				M3OnConnectionFail (cause);
			}
		}
		public override void OnDisconnectedFromPhoton ()
		{
			base.OnDisconnectedFromPhoton ();
			Debug.Log("OnDisconnectedFromPhoton()" + PhotonNetwork.networkingPeer.State);
			if (M3OnDisconnectedFromPhoton != null) 
			{
				M3OnDisconnectedFromPhoton ();
			}
		}
		public override void OnPhotonMaxCccuReached ()
		{
			base.OnPhotonMaxCccuReached ();
			Debug.Log ("OnPhotonMaxCccuReached()");
			if (M3OnPhotonMaxCccuReached != null) {
				M3OnPhotonMaxCccuReached ();
			}
		}

	}
}
