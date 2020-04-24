using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallConnect.Utility;

namespace BLB.TentDemo
{
    [FullSerializer.fsObject("v1")]
    public class TentDemoSaveData
    {
        public DFPosition TentMapPixel;
        public bool TentDeployed;
        public Vector3 TentPosition;
        public Quaternion TentRotation;
        public Matrix4x4 TentMatrix;
    }


    //this class initializes the mod.
    public class TentDemoModLoader : MonoBehaviour, IHasModSaveData
    {

        private static DFPosition TentMapPixel = null;
        private static bool TentDeployed = false;
        private static Vector3 TentPosition;
        private static Quaternion TentRotation;
        private static GameObject Tent = null;        
        private static Matrix4x4 TentMatrix;

        public Type SaveDataType
        {
            get { return typeof(TentDemoSaveData); }
        }

        public object NewSaveData()
        {
            return new TentDemoSaveData
            {
                TentMapPixel = new DFPosition(),
                TentDeployed = false,
                TentPosition = new Vector3(),
                TentRotation = new Quaternion(),
                TentMatrix = new Matrix4x4()
            };
        }

        public object GetSaveData()
        {
            return new TentDemoSaveData
            {
                TentMapPixel = TentMapPixel,
                TentDeployed = TentDeployed,
                TentPosition = TentPosition,
                TentRotation = TentRotation,
                TentMatrix = TentMatrix
            };
        }

        public void RestoreSaveData(object saveData)
        {
            TentDemoSaveData tentDemoSaveData = (TentDemoSaveData)saveData;
            TentMapPixel = tentDemoSaveData.TentMapPixel;
            TentDeployed = tentDemoSaveData.TentDeployed;
            TentPosition = tentDemoSaveData.TentPosition;
            TentRotation = tentDemoSaveData.TentRotation;
            TentMatrix = tentDemoSaveData.TentMatrix;

            if(TentDeployed) {
                DeployTent(true);
            }
        }

        public static Mod mod;
		public static GameObject go;

        public static TentDemoSaveData SaveDataInterface;

        public const int tentModelID = 41606;
        public const int templateIndex_Tent = 515;

        //like in the last example, this is used to setup the Mod.  This gets called at Start state.
        [Invoke]
        public static void InitAtStartState(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            mod.SaveDataInterface = go.AddComponent<TentDemoModLoader>();

            Debug.Log("Started setup of : " + mod.Title);

            DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHander(templateIndex_Tent, UseTent);
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(templateIndex_Tent, ItemGroups.UselessItems2);
            PlayerActivate.RegisterCustomActivation(mod, 41606, PackUpTent);

            mod.IsReady = true;
        }

        private static bool UseTent(DaggerfallUnityItem item, ItemCollection collection)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside == true) {
                ShowMessageBox("You can not pitch your tent indoors.");
                return false;
            } else if (TentDeployed == false) {
                item.LowerCondition(1, GameManager.Instance.PlayerEntity, collection);
                DeployTent();
                return true;
            } else {
                ShowMessageBox("You have already pitched your tent.");
                return false;
            }
        }

        private static void DeployTent(bool fromSave = false) {
            if(fromSave == false) {
                TentMapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;

                GameObject player = GameManager.Instance.PlayerObject;
                PlayerMotor playerMotor = player.GetComponent<PlayerMotor>();
                // Find ground position below player
                TentPosition = playerMotor.FindGroundPosition();

                //Calculate / retrieve tent position, rotation and facing direction
                TentPosition = TentPosition + (player.transform.forward * 3);
                TentRotation = player.transform.rotation;
                TentMatrix = player.transform.localToWorldMatrix;
            }
            //Attempt to load a model replacement
            Tent = MeshReplacement.ImportCustomGameobject(tentModelID, null, TentMatrix);
            if(Tent == null) {
                Tent = GameObjectHelper.CreateDaggerfallMeshGameObject(tentModelID, null);
            }
            //Set the model's position in the world
            Tent.transform.SetPositionAndRotation(TentPosition, TentRotation);
            Tent.SetActive(true);
            TentDeployed = true;
        }

        private static void PackUpTent(RaycastHit hit) {
            if (hit.transform.gameObject.GetInstanceID() == Tent.GetInstanceID()) {
                string[] buttons = new string[]{"PACKUPTENT","REST"};
                string message = "What do you want to do?";
                ShowMessageBox(message, false, buttons);
            } else {
                ShowMessageBox("This is not your tent, you filthy thief!");
            }
        }

        private static void ShowMessageBox(string message, bool clickAnywhereToClose = true, string[] buttons = null)
        {
            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager);
            messageBox.ClickAnywhereToClose = clickAnywhereToClose;
            messageBox.ParentPanel.BackgroundColor = Color.clear;
            messageBox.ScreenDimColor = new Color32(0, 0, 0, 0);

            messageBox.SetText(message);

            if (buttons != null) {
                for (int i = 0; i < buttons.Length; i++) {
                    messageBox.AddCustomButton(99 + i, buttons[i], false);
                }
                messageBox.OnCustomButtonClick += Tent_messageBox_OnButtonClick;
            }

            messageBox.Show();
        }

        public static void Tent_messageBox_OnButtonClick(DaggerfallMessageBox sender, string messageBoxButton) {
            if (messageBoxButton == "REST") {
                IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
                uiManager.PushWindow(new DaggerfallRestWindow(uiManager, true));
            } else if (messageBoxButton == "PACKUPTENT") {
                Destroy(Tent);
                Tent = null;
                TentDeployed = false;
                TentMatrix = new Matrix4x4();
                sender.CloseWindow();
            }
        }

        [Invoke(StateManager.StateTypes.Game)]
        public static void InitAtGameState(InitParams initParams)
        {

        }
    }
}
