using M3PUN;
using Photon;
using GemsFrontier;

namespace M3PUN {
    public class StateReceivedChallengeBase : StateBase {
        protected Events.PrivateChallengeArgs privateChallengeArgs;
        protected EventListenerForPrivateChallenge eventListener;
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

        public override void OnStateEnter() {
            base.OnStateEnter();
            #if !UNITY_WEBGL
                if (M3Utils.Instance.IsFizzOpen) {
                    FIZZ.FIZZUISDK.Instance.Close();
                }
            #endif

            if(_data == null) {
                Helpers.Utility.LogError("_data shouldn't be null in OnStateEnter of " + this.GetType().ToString());
            } else {
                privateChallengeArgs = (Events.PrivateChallengeArgs) _data;
            }

            eventListener = GetComponent<EventListenerForPrivateChallenge>();

            if(!eventListener.enabled) {
                eventListener.enabled = true;
            }

            
            ConnectionController.Instance.M3OnConnectedToMaster += _onConnectedToMaster;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected += _onPlayerDisconnected;

            //EventManager Hookup
            Events.EventManager.Instance.PrivateChallengeBusyHandler += _onPrivateChallengeBusyHandler;
            Events.EventManager.Instance.PrivateChallengeIgnoreHandler += _onPrivateChallengeIgnoreHandler;
            Events.EventManager.Instance.PrivateChallengeYesHandler += _onPrivateChallengeYesHandler;
            Events.EventManager.Instance.PrivateChallengeNoHandler += _onPrivateChallengeNoHandler;
            Events.EventManager.Instance.PrivateChallengeStartBattleHandler += _onPrivateChallengeStartBattleHandler;

            DoCloseAndHideTheRoom();    //do not let other people to come to this room
        }
        
        public override void OnStateExit() {
            
            ConnectionController.Instance.M3OnConnectedToMaster -= _onConnectedToMaster;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected -= _onPlayerDisconnected;
            //EventManager Hookup
            Events.EventManager.Instance.PrivateChallengeBusyHandler -= _onPrivateChallengeBusyHandler;
            Events.EventManager.Instance.PrivateChallengeIgnoreHandler -= _onPrivateChallengeIgnoreHandler;
            Events.EventManager.Instance.PrivateChallengeYesHandler -= _onPrivateChallengeYesHandler;
            Events.EventManager.Instance.PrivateChallengeNoHandler -= _onPrivateChallengeNoHandler;
            Events.EventManager.Instance.PrivateChallengeStartBattleHandler -= _onPrivateChallengeStartBattleHandler;

            base.OnStateExit();
        }


        protected void DoGoBackToWaitState() {
            VersusScreen.Instance.HideCancelButton();
            VersusScreen.Instance.StopAnimation();
            ConnectionController.Instance.LeaveRoom();
        }


        void DoTimeOutChallenge(float t=0) {
            CancelInvoke("DoTimeOutChallenge");

            if(t == 0) {
                popup = EventListenerForPrivateChallenge.ShowUserIsBusyPopup(privateChallengeArgs.OpponentNickName);
                VersusScreen.Instance.HideCancelButton();
                VersusScreen.Instance.StopAnimation();
                if(ConnectionController.Instance.inRoom) {
                    ConnectionController.Instance.LeaveRoom();
                } else {
                    _onConnectedToMaster();
                }
            } else if (t > 0) {
                Invoke("DoTimeOutChallenge", t);
            }
        }
        

        void DoCloseAndHideTheRoom() {
            ConnectionController.Instance.room.IsOpen = false;
            //ConnectionController.Instance.room.IsVisible = false;
        }
        void DoOpenAndPublishTheRoom() {
            ConnectionController.Instance.room.IsOpen = true;
            //ConnectionController.Instance.room.IsVisible = true;
        }

        protected virtual void DoUserSaidNo() {
            eventListener.SendFriendlyBattleChallengeNO(privateChallengeArgs.BattleType);
            DoGoBackToWaitState();
        }

        protected virtual void DoUserSaidYes() {
            eventListener.SendFriendlyBattleChallengeYES(privateChallengeArgs.BattleType);
            VersusScreen.Instance.StartAnimationForFriendlyChallenge();
        }

        protected void _onConnectedToMaster() {
            StateMachine.Instance.MakeTransition(typeof(StateInitialize));
        }
        //EventManager Hookup
        protected void _onDisconnectedFromPhoton() {
            EventListenerForPrivateChallenge.ShowDisconnectedFromRoom();
            DoGoBackToWaitState();
        }

        void _onPlayerDisconnected(PhotonPlayer player) {
            DoTimeOutChallenge(0);
        }
        protected virtual void _onPrivateChallengeBusyHandler(object sender, Events.ChallengeArgs args) {
            DoTimeOutChallenge(0);
        }

        protected virtual void _onPrivateChallengeIgnoreHandler(object sender, Events.ChallengeArgs args) {
            DoTimeOutChallenge(0);
        }

        protected virtual void _onPrivateChallengeYesHandler(object sender, Events.ChallengeArgs args) {
            //We just sent a Yes to Opponent
        }

        protected virtual void _onPrivateChallengeNoHandler(object sender, Events.ChallengeArgs args) {
            //We just sent a No to Opponent
        }

        protected virtual void _onPrivateChallengeStartBattleHandler(object sender, Events.ChallengeArgs args) {
            M3GameCache.SetBattleType(privateChallengeArgs.BattleType);
            StateMachine.Instance.MakeTransition(typeof(StateGoForOnlineBattle));
        }
    }
}