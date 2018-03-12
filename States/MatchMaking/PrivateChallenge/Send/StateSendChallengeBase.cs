using M3PUN;
using Photon;
using GemsFrontier;
using System;
using System.Collections.Generic;

namespace M3PUN {

    public class StateSendChallengeBase : StateBase {

        public static string PRIVATE_CHALLENGE_TYPE = "PRIVATE_CHALLENGE_TYPE";
        protected AlertPopup _popup;
        protected AlertPopup popup {
            get { return _popup;}
            set {
                    if(_popup != null) {
                        _popup.HideAndDestroy();
                    }
                    _popup = value;
            }
        }

        protected Events.PrivateChallengeArgs privateChallengeArgs;
        protected EventListenerForPrivateChallenge eventListener;

        bool joinLobbyAfterLeave = false;
        bool searchAfterJoinLobby = false;
        bool goToWaitState = false;
        public override void OnStateEnter() {
            base.OnStateEnter();

            if(_data == null) {
                Helpers.Utility.LogError("_data shouldn't be null in OnStateEnter of " + this.GetType().ToString());
            } else {
                Dictionary<string, object> parameters = new Dictionary<string,object>();
                parameters = _data as Dictionary<string, object>;

                if(parameters.ContainsKey(PRIVATE_CHALLENGE_TYPE)) {
                    privateChallengeArgs = (Events.PrivateChallengeArgs) parameters[PRIVATE_CHALLENGE_TYPE];
                }
            }
            eventListener = GetComponent<EventListenerForPrivateChallenge>();
            if(!eventListener.enabled) {
                eventListener.enabled = true;
            }


            ConnectionController.Instance.M3OnConnectedToMaster += _onConnectedToMaster;
            ConnectionController.Instance.M3OnJoinedLobby += _onJoinedLobby;
            ConnectionController.Instance.M3OnJoinedRoom += _onJoinedRoom;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected += _onPlayerDisconnected;
            ConnectionController.Instance.M3OnPhotonJoinRoomFailed += _onPhotonRandomJoinFailed;
            ConnectionController.Instance.M3OnPhotonRandomJoinFailed += _onPhotonRandomJoinFailed;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnectedFromPhoton;

            //EventManager Hookup
            Events.EventManager.Instance.PrivateChallengeMessageHandler += _onPrivateChallengeMessage;
            Events.EventManager.Instance.PrivateChallengeBusyHandler += _onPrivateChallengeBusyHandler;
            Events.EventManager.Instance.PrivateChallengeIgnoreHandler += _onPrivateChallengeIgnoreHandler;
            Events.EventManager.Instance.PrivateChallengeYesHandler += _onPrivateChallengeYesHandler;
            Events.EventManager.Instance.PrivateChallengeNoHandler += _onPrivateChallengeNoHandler;
            Events.EventManager.Instance.PrivateChallengeStartBattleHandler += _onPrivateChallengeStartBattleHandler;



            if(ConnectionController.Instance.ConnectedAndOnline) {
                DoTryToJoinRoomWithUserId(privateChallengeArgs.OpponentUserId);
            } else {
                Helpers.Utility.LogError("Not Connected to PhotonServer" + this.GetType().ToString());
                DoHideVersusScreen();
                StateMachine.Instance.MakeTransition(typeof(StateInitialize));
            }
            
        }

        public override void OnStateExit() {
            privateChallengeArgs = null;
            
            ConnectionController.Instance.M3OnConnectedToMaster -= _onConnectedToMaster;
            ConnectionController.Instance.M3OnJoinedLobby -= _onJoinedLobby;
            ConnectionController.Instance.M3OnJoinedRoom -= _onJoinedRoom;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected -= _onPlayerDisconnected;
            ConnectionController.Instance.M3OnPhotonJoinRoomFailed -= _onPhotonRandomJoinFailed;
            ConnectionController.Instance.M3OnPhotonRandomJoinFailed -= _onPhotonRandomJoinFailed;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnectedFromPhoton;

            //EventManager Hookup
            Events.EventManager.Instance.PrivateChallengeMessageHandler -= _onPrivateChallengeMessage;
            Events.EventManager.Instance.PrivateChallengeBusyHandler -= _onPrivateChallengeBusyHandler;
            Events.EventManager.Instance.PrivateChallengeIgnoreHandler -= _onPrivateChallengeIgnoreHandler;
            Events.EventManager.Instance.PrivateChallengeYesHandler -= _onPrivateChallengeYesHandler;
            Events.EventManager.Instance.PrivateChallengeNoHandler -= _onPrivateChallengeNoHandler;
            Events.EventManager.Instance.PrivateChallengeStartBattleHandler -= _onPrivateChallengeStartBattleHandler;

            base.OnStateExit();
        }

        void DoHideVersusScreen() {
            VersusScreen.Instance.StopAnimation();
        }

        protected void DoTryToJoinRoomWithUserId(string userId) {
            UserProfile up = GameState.Instance.Player.profile;
            if(up.UserId == userId) {//Challenging self
                //why'd you like to do that?
                Helpers.Utility.LogMessage("DoTryToJoinRoomWithUserId: Challenging own user?? Why would someone do that?" + this.GetType().ToString());
                popup = EventListenerForPrivateChallenge.ShowCannotChallengeSelfPopup(up.Nick);
                StateMachine.Instance.MakeTransition(typeof(StateWaitForChallenge));
            } else {
                if(ConnectionController.Instance.insideLobby) {
                    DoSearch(userId);
                } else {
                    joinLobbyAfterLeave = true;
                    searchAfterJoinLobby = true;
                    ConnectionController.Instance.LeaveRoom();
                }
            }
        }

        protected void DoGoBackToWaitState() {
            joinLobbyAfterLeave = false;
            searchAfterJoinLobby = false;
            goToWaitState = true;
            if(ConnectionController.Instance.inRoom) {
                ConnectionController.Instance.LeaveRoom();
            } else {
                _onConnectedToMaster();
            }
            VersusScreen.Instance.HideCancelButton();
            VersusScreen.Instance.StopAnimation();
        }

        void DoSearch(string userId) {
            string query = string.Format ("{0} = \'{1}\' AND {2} = \'{3}\'", Utils.CTECH_PUN_ROOM_USER_NAME, userId, Utils.CTECH_PUN_ROOM_CHALLENGE_MODE, Utils.STR_PRIVATE);
            if(UserPrefHelper.IsLogEnabled) {
                Helpers.Utility.LogMessage(this.GetType().ToString(), "DoSearch", query);
            }

            ConnectionController.Instance.TryToJoinRoomWithSQLQuery(query);
        }

        void DoTimeOutChallenge(float t=0) {
            CancelInvoke("DoTimeOutChallenge");

            if(t == 0) {
                popup = EventListenerForPrivateChallenge.ShowUserIsBusyPopup(privateChallengeArgs.OpponentNickName);
                DoGoBackToWaitState();
            } else if (t > 0) {
                Invoke("DoTimeOutChallenge", t);
            }
        }

        void DoSendStartBattle() {
            DoTimeOutChallenge(-1);
            Invoke("SendStartBattleCall", 2.0f);
        }

        void SendStartBattleCall() {
            DoTimeOutChallenge(20);
            eventListener.SendFriendlyBattleStartSignal(privateChallengeArgs.BattleType);
        }
        void DoShowDeclinedPopupAndGoBack() {
            DoTimeOutChallenge(-1);
            popup = EventListenerForPrivateChallenge.ShowUserDeclinedPopup(privateChallengeArgs.OpponentNickName);
            DoGoBackToWaitState();
        }

        //Bound Callbacks
         protected void _onConnectedToMaster() {
            if(joinLobbyAfterLeave) {
                joinLobbyAfterLeave = false;
                ConnectionController.Instance.JoinLobby();
            } else if(goToWaitState) {
                StateMachine.Instance.MakeTransition(typeof(StateInitialize));
            }
        }


        void _onJoinedLobby () {
            if(searchAfterJoinLobby) {
                searchAfterJoinLobby = false;
                DoSearch(privateChallengeArgs.OpponentUserId);
            }
        }
        
        void _onJoinedRoom () {
            //UserFound
            eventListener.SendFriendlyBattleChallenge(privateChallengeArgs.BattleType);
        }


        void _onPhotonRandomJoinFailed(object[] codeAndMsg) {
            if(UserPrefHelper.IsLogEnabled) {
                Helpers.Utility.LogMessage(this.GetType().ToString(), "_onPhotonRandomJoinedFailed", codeAndMsg[0] + " " + codeAndMsg[1]);
            }

            System.Action HideVersusScreen = ()=> {
                DoHideVersusScreen();
            };

            string code = codeAndMsg[0].ToString();
            switch(code) {
                case M3Constants.PUN_ERROR_CODE_SLOT_EXCEED_MAX_PLAYERS:
                case M3Constants.PUN_ERROR_CODE_EXPECTED_USERS:
                case M3Constants.PUN_ERROR_CODE_GAME_CLOSED:
                case M3Constants.PUN_ERROR_CODE_GAME_FULL:
                case M3Constants.PUN_ERROR_CODE_USER_NOT_EXPECTED:
                case M3Constants.PUN_ERROR_CODE_USER_EXCLUDED:
                //This user was not expected to Join
                popup = EventListenerForPrivateChallenge.ShowUserIsBusyPopup(privateChallengeArgs.OpponentNickName, HideVersusScreen);
                break;

                case M3Constants.PUN_ERROR_CODE_NO_RANDOM_MATCH:
                case M3Constants.PUN_ERROR_CODE_ROOM_DOESNT_EXIST:
                default:
                popup = EventListenerForPrivateChallenge.ShowUserIsNotAvailablePopup(privateChallengeArgs.OpponentNickName, HideVersusScreen);
                
                break;
            }

            DoGoBackToWaitState();
        }

        void _onPlayerDisconnected (PhotonPlayer otherPlayer) {
            popup = EventListenerForPrivateChallenge.ShowUserIsNotAvailablePopup(privateChallengeArgs.OpponentNickName);
            DoGoBackToWaitState();            
        }
        protected void _onDisconnectedFromPhoton () {
            Helpers.Utility.LogMessage("_onDisconnectedFromPhoton: Disconnected From Photon, Reconnection not allowed in this state, going back." + this.GetType().ToString());
            EventListenerForPrivateChallenge.ShowDisconnectedFromRoom();
            DoGoBackToWaitState();
        }


        //EventManager Hookup
        void _onPrivateChallengeMessage(object sender, Events.ChallengeArgs args) {
            VersusScreen.Instance.SetLoadingTip(string.Format(LocalizationManager.Instance.GetString(LocalizationKeys.USER_CHALLENGE_WAITING_FOR_ACCEPT), privateChallengeArgs.OpponentNickName));
            DoTimeOutChallenge(ServerConfigurableValues.Instance.BattleChallengeRoomWait);
        }

        void _onPrivateChallengeBusyHandler(object sender, Events.ChallengeArgs args) {
            DoTimeOutChallenge(0);
        }

        void _onPrivateChallengeIgnoreHandler(object sender, Events.ChallengeArgs args) {
            DoTimeOutChallenge(0);
        }

        void _onPrivateChallengeYesHandler(object sender, Events.ChallengeArgs args) {
            DoSendStartBattle();
        }

        void _onPrivateChallengeNoHandler(object sender, Events.ChallengeArgs args) {
            DoShowDeclinedPopupAndGoBack();
        }

        void _onPrivateChallengeStartBattleHandler(object sender, Events.ChallengeArgs args) {
            M3GameCache.SetBattleType(privateChallengeArgs.BattleType);
            StateMachine.Instance.MakeTransition(typeof(StateGoForOnlineBattle));
        }


    }
}