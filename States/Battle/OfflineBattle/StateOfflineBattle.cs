using GemsFrontier;
using UnityEngine;
using M3PUN;
using Photon;

namespace M3PUN {
    public class StateOfflineBattle : StateBase {
        
        BattleField battleField;

        public override void OnStateEnter() {
            base.OnStateEnter();
            battleField = GameObject.FindGameObjectWithTag ("BattleField").GetComponent<BattleField> ();

            ConnectionController.Instance.M3OnJoinedRoom += _onJoinedRoom;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnected;
            ConnectionController.Instance.M3OnConnectedToMaster += _onConnected;
            

            if(!ConnectionController.Instance.offlineMode) {
                Debug.LogError("Photon is not currently Offline, this M3PUN State Shouldn't load. Handling exceptional case.");
                if(ConnectionController.Instance.ConnectedAndOnline) {
                    ConnectionController.Instance.Disconnect();
                } else {
                    ConnectionController.Instance.SwitchToOfflineMode();
                }
            } else {
                DoJoinOfflineBattleRoom();
            }
        }

        public override void OnStateExit() {
            ConnectionController.Instance.M3OnJoinedRoom -= _onJoinedRoom;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnected;
            ConnectionController.Instance.M3OnConnectedToMaster -= _onConnected;

            base.OnStateExit();
        }


        //Custom Methods
        void DoJoinOfflineBattleRoom() {
            
            if(ConnectionController.Instance.inRoom) {
                _onJoinedRoom();
                return;
            }

            RoomOptions roomOpt = new RoomOptions();
            roomOpt.MaxPlayers = 2;
            roomOpt.IsOpen = false;
            roomOpt.IsVisible = false;
            string prefix = "";
            if (battleField.firstBattleTutorial) {
                prefix = M3Constants.TUTORIAL_BATLE_PREFIX + "_1_";
            } else if (battleField.secondBattleTutorial) {
                prefix = M3Constants.TUTORIAL_BATLE_PREFIX + "_2_";
            } else if (battleField.thirdBattleTutorial) {
                prefix = M3Constants.TUTORIAL_BATLE_PREFIX + "_3_";
            } else if (battleField.fourthBattleTutorial) {
                prefix = M3Constants.TUTORIAL_BATLE_PREFIX + "_4_";
            } else {
                prefix = "waefau0-";
            }
            string offlineBattleId = prefix + System.Guid.NewGuid().ToString();

            ConnectionController.Instance.JoinOrCreateRoom(offlineBattleId, roomOpt, null);
        }

        //Bound Callbacks

        void _onDisconnected() {
            //Switch to Offline Mode
            ConnectionController.Instance.SwitchToOfflineMode();
        }
        void _onConnected() {
            //switched to Offline mode
            if(ConnectionController.Instance.offlineMode) {
                DoJoinOfflineBattleRoom();
            } else if (ConnectionController.Instance.ConnectedAndOnline) {
                ConnectionController.Instance.Disconnect();
            }
        }
        void _onJoinedRoom() {
            //now we're inside an offline room, we can join the battle as connected.
            VersusScreen.Instance.OnFailHideScreen(-1);
           battleField.EntryPointConnectedToPhoton();
        }
    }

    
}