using M3PUN;
using Photon;
using GemsFrontier;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M3PUN {
    public class StateWaitForChallenge : StateOnlineBase {
        bool findBattlePartner = false;
        public static string FIND_BATTLE_PARTNER = "FIND_BATTLE_PARTNER";
        protected EventListenerForPrivateChallenge eventListener;

        public override void OnStateEnter() {
            base.OnStateEnter();
            challengeMode = Utils.STR_PRIVATE;
            ConnectionController.Instance.M3OnJoinedRoom += _onJoinedRoom;
            ConnectionController.Instance.M3OnJoinedLobby += _onJoinedLobby;
            ConnectionController.Instance.M3OnPhotonJoinRoomFailed += _onJoinRoomFailed;
            

            Events.EventManager.Instance.FindBattlePartnerHandler += _OnFindBattlePartner;
            Events.EventManager.Instance.ChallengeButtonHandler += _OnChallengeButtonPressed;
            Events.EventManager.Instance.UpdatePhotonRoomPropertiesHandler += _onUpdateRoomProperties;

            //Private Challenge
            eventListener = GetComponent<EventListenerForPrivateChallenge>();
            if(!eventListener.enabled) {
                eventListener.enabled = true;
            }
            ConnectionController.Instance.M3OnPhotonPlayerConnected += _onPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected += _onPlayerDisconnected;
            Events.EventManager.Instance.PrivateChallengeMessageHandler += _onPrivateChallengeMessage;

            Events.EventManager.Instance.MainMenuLoadCompleteHandler += _onMainMenuSceneLoaded;
            
            if(_data == null) {
                //no data was passed.
            } else {
                Dictionary<string, object> parameters = new Dictionary<string,object>();
                parameters = _data as Dictionary<string, object>;

                if(parameters.ContainsKey(FIND_BATTLE_PARTNER)) {
                    findBattlePartner = (bool) parameters[FIND_BATTLE_PARTNER];
                }
            }

            if(ConnectionController.Instance.connected) {
                _onConnectedToMaster();
            } else {
                ConnectionController.Instance.Connect();
            }
        }
        
        public override void OnStateExit() {
            ConnectionController.Instance.M3OnJoinedRoom -= _onJoinedRoom;
            ConnectionController.Instance.M3OnJoinedLobby -= _onJoinedLobby;
            ConnectionController.Instance.M3OnPhotonJoinRoomFailed -= _onJoinRoomFailed;


            
            Events.EventManager.Instance.FindBattlePartnerHandler -= _OnFindBattlePartner;
            Events.EventManager.Instance.ChallengeButtonHandler -= _OnChallengeButtonPressed;
            Events.EventManager.Instance.UpdatePhotonRoomPropertiesHandler -= _onUpdateRoomProperties;

            //Private Challenge

            ConnectionController.Instance.M3OnPhotonPlayerConnected -= _onPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected -= _onPlayerDisconnected;
            Events.EventManager.Instance.PrivateChallengeMessageHandler -= _onPrivateChallengeMessage;

            Events.EventManager.Instance.MainMenuLoadCompleteHandler -= _onMainMenuSceneLoaded;

            base.OnStateExit();
        }
        void _OnChallengeButtonPressed(object sender, Events.PrivateChallengeArgs privateChallenge) {
            //UnityEngine.GameObject button = sender as UnityEngine.GameObject;

            BattleType challengeType = privateChallenge.BattleType;
            if((challengeType & BattleType.FriendlyMaskForCheck) > 0) {
                //This is a Friendly Battle
                if((challengeType & BattleType.FacebookFriendly) > 0) {
                    VersusScreen.Instance.StartAnimationForFriendlyChallenge();
                    
                    StateMachine.Instance.MakeTransition(typeof(StateSendChallengeFacebook), 
                        new Dictionary<string, object>() {
                        {StateSendChallengeBase.PRIVATE_CHALLENGE_TYPE, privateChallenge }}
                    );
                } else if ((challengeType & BattleType.GuildPrivateFriendly) > 0) {
                    VersusScreen.Instance.StartAnimationForFriendlyChallenge();
                    StateMachine.Instance.MakeTransition(typeof(StateSendChallengeGuild), 
                        new Dictionary<string, object>() {
                        {StateSendChallengeBase.PRIVATE_CHALLENGE_TYPE, privateChallenge }}
                    );
                } else if ((challengeType & BattleType.GuildFriendly) > 0) {
                   Helpers.Utility.LogMessage("_OnChallengeButtonPressed: Guild Challenge is not supported yet.");
                } else {
                    Helpers.Utility.LogMessage("_OnChallengeButtonPressed: This Challenge Type is not supported yet.");
                }
            }
        }
        void _OnFindBattlePartner(object sender,System.EventArgs args) {
            findBattlePartner = true;

            VersusScreen.Instance.StartAnimation();
            M3GameCache.SetBattleType(BattleType.RandomPVP);
            
            M3PUN.Utils.UpdatePhotonPlayerProperties();

            if(ConnectionController.Instance.ConnectedAndOnline) {
                DoFindMatch();
            } else {
                VersusScreen.Instance.StartAnimation();
                StateMachine.Instance.MakeTransition(typeof(StateGoForAIBattle));
            }
        }
        
        void DoFindMatch() {
            if(!VersusScreen.Instance.AnimContainer.isActiveAndEnabled) {
                VersusScreen.Instance.StartAnimation();
            }
            StartCoroutine(_doFindMatch());
        }

        IEnumerator _doFindMatch() {
            findBattlePartner = false;
            int waitSeconds = 0;
            bool doOffline = false;
            while(true) {
                if(ConnectionController.Instance.inRoom) {
                    break;
                } else if (waitSeconds >= 4) {
                    doOffline = true;
                    break;
                }
                waitSeconds++;
                yield return new WaitForSeconds(1f);
            }

            if(doOffline) {
                StateMachine.Instance.MakeTransition(typeof(StateGoForAIBattle));
            } else {
                StateMachine.Instance.MakeTransition(typeof(StateRandomMatchMakingHost));
            }
        }

        protected override void DoInitNewRoomOptionsAndJoinRoom() {
            
            if(ConnectionController.Instance.inRoom) {
                if (ConnectionController.Instance.isMasterClient) {
                    DoUpdateRoomProperty(Utils.CTECH_PUN_ROOM_CHALLENGE_MODE, challengeMode);
                    DoOpenAndPublishTheRoom();
                    _onJoinedRoom();
                } else {
                    ConnectionController.Instance.LeaveRoom();
                }
            } else {
                base.DoInitNewRoomOptionsAndJoinRoom();
            }
        }

        void DoConnect() {
            if(ConnectionController.Instance.ConnectedAndOnline) {
                //Photon is connected and it is not Offline. Using custom property otherwise PhotonNetwork.connected is true in offline mode as well
                _onConnectedToMaster();
            } else {
                //Either the game is disconnected or offline
                if(ConnectionController.Instance.connectionState != ConnectionState.Connecting) {
                    reconnecting = ConnectionController.Instance.ReconnectAndRejoin();
                    if(!reconnecting) {
                        if(!ConnectionController.Instance.Reconnect()) {
                            ConnectionController.Instance.Connect(M3Constants.PHOTON_GAME_VERSION);
                        }
                    }
                }
            }
        }
        void DoCloseAndHideTheRoom() {
            Helpers.Utility.LogMessage("Hiding the Room");
            if(ConnectionController.Instance.room != null) {
                ConnectionController.Instance.room.IsOpen = false;
                //ConnectionController.Instance.room.IsVisible = false;
            }
        }
        void DoOpenAndPublishTheRoom() {
            if(ConnectionController.Instance.room != null) {
                Helpers.Utility.LogMessage("Opening the Room");
                ConnectionController.Instance.room.IsOpen = true;
                //ConnectionController.Instance.room.IsVisible = true;
            }
        }

        void _onMainMenuSceneLoaded(object sender, System.EventArgs args) {
            DoOpenAndPublishTheRoom();
            GemsFrontier.UserProfile up = GemsFrontier.GameState.Instance.Player.profile;
            if(up != null) {
                DoUpdateRoomProperty(Utils.CTECH_PUN_ROOM_PROPERTIES_TROPHIES, up.Trophies.ToString());
            }
            M3PUN.Utils.UpdatePhotonPlayerProperties();
        }

        //Bound Callbacks
        protected override void _onConnectedToMaster() {
            if(disconnectionCause == DisconnectCause.AuthenticationTicketExpired || disconnectionCause == DisconnectCause.InvalidAuthentication) {
                //do nothing
            } else {
                if(!ConnectionController.Instance.insideLobby) {
                    DoJoinLobby();
                } else {
                    _onJoinedLobby();
                }
            }
            base._onConnectedToMaster();

        }
        void _onJoinedLobby() {
            if(!ConnectionController.Instance.inRoom) {
                DoInitNewRoomOptionsAndJoinRoom();
            } else {
                DoOpenAndPublishTheRoom();
            }
        }
        void _onJoinedRoom() {
            M3PUN.Utils.PrintRoomDetails("StateWaitForChallenge", "_onJoinedRoom");
            bool isBatteFieldScene = (SceneManagerHelper.ActiveSceneName == SceneUtil.GetSceneName(SceneType.BattleField));
            
            if(isBatteFieldScene) { //if we're still in battlefield, hide the room
                DoCloseAndHideTheRoom();
            }

            if(findBattlePartner) {
                DoFindMatch();
            }

            if(LoadingView.Instance.IsVisible) {
                LoadingView.SharedInstance ().HideTransition();
            }
        }

        void _onPlayerConnected(PhotonPlayer player) {
            DoCloseAndHideTheRoom();
        }

        void _onPlayerDisconnected(PhotonPlayer player) {
            if(ConnectionController.Instance.room != null && ConnectionController.Instance.room.PlayerCount == 1) {
                DoOpenAndPublishTheRoom();
            }
        }

        void _onJoinRoomFailed(object[] codeAndMsg) {
            Helpers.Utility.LogMessage("StateWaitForChallenge _onJoinedRoomFailed: code:" + codeAndMsg[0].ToString() + " msg: " + codeAndMsg[1].ToString());
            string code = codeAndMsg[0].ToString();
            switch(code) {
                case M3Constants.PUN_ERROR_CODE_CCU_LIMIT_REACHED:
                //CCU limit reach cannot join server right now.
                StateMachine.Instance.MakeTransition(typeof(StateOffline), 
                        new Dictionary<string, object>() {
                        {StateWaitForChallenge.FIND_BATTLE_PARTNER, findBattlePartner }}
                    );
                break;

                default:
                    DoInitNewRoomOptionsAndJoinRoom();
                break;

            }

        }
        
        void _onUpdateRoomProperties(object sender, Events.PhotonRoomPropertiesUpdate propset) {
            DoUpdateRoomProperty(propset);
        }

        void _onPrivateChallengeMessage(object sender, Events.ChallengeArgs args) {
            Events.PrivateChallengeArgs privateChallengeArgs = new Events.PrivateChallengeArgs(args.OtherPlayer.M3UserId(), args.OtherPlayer.M3NickName(), args.OtherPlayer.M3Trophies().ToString(), args.OtherPlayer.M3GuildName(), args.OtherPlayer.M3FacebookId(), args.BattleType);
            if((args.BattleType & BattleType.FacebookMaskForCheck) > 0) {
                StateMachine.Instance.MakeTransition(typeof(StateReceivedChallengeFacebook), privateChallengeArgs);
            } else if((args.BattleType & BattleType.GuildMaskForCheck) > 0) {
                StateMachine.Instance.MakeTransition(typeof(StateReceivedChallengeGuild), privateChallengeArgs);
            } else {
                //mode not supported
                ConnectionController.Instance.KickPlayer(args.OtherPlayer); //kick Player
            }
        }
    }
}