using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game;
using VRage.Common.Utils;
using VRageMath;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Game.ModAPI;
using Sandbox.Game.EntityComponents;
using VRage.Input;
using Sandbox.Game.GameSystems;
using VRage.Game.VisualScripting;
using Sandbox.Game.World;
using Sandbox.Game.Components;
using VRageRender.Animations;
using SpaceEngineers.Game.ModAPI;
using ProtoBuf;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using Sandbox.ModAPI.Weapons;
using System.ComponentModel.Design;
using PSYCHO;
using MyVisualScriptLogicProvider = Sandbox.Game.MyVisualScriptLogicProvider;

using Sandbox.Game.Lights;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ObjectBuilders.Definitions;

using IMyControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;
using Sandbox.Game.Entities.Character.Components;
using VRage.Game.SessionComponents;
using System.Data;
using VRage.Game.VisualScripting.Missions;

// Huge thanks to Digi for all his mods!
// This code uses a small part of the Camera Panning mod code to auto-align camera back to front.
// AccidentallyTheCable and Me had this idea for some time. I'm sure we're not the only ones that wanted such a feature.

namespace PSYCHO.AutoCam
{
    //[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    //[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]

    public class AutoCamLogic : MySessionComponentBase
    {
        int tick = 0;

        int alignTick = 60;
        int alignAfter = 60;

        float shipCamHeightOffset = -15f;

        bool isInFirstPersonView = false;
        IMyControllableEntity ControlledObject;
        IMyControllableEntity RefController;

        bool CameraAutoAlignEnabled = true;

        /*
        public override void HandleInput()
        {
            try
            {
                if (MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible)
                    return;

                CamAlignHandler();
            }
            catch (Exception e)
            {
                //... hm?
            }
        }
        */

        public void CamAlignHandler()
        {
            try
            {
                if (MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.LOOKAROUND))
                {
                    if (MyAPIGateway.Input.IsGameControlReleased(MyControlsSpace.CAMERA_MODE))
                    {
                        CameraAutoAlignEnabled = !CameraAutoAlignEnabled;
                        if (CameraAutoAlignEnabled)
                        {
                            MyAPIGateway.Utilities.ShowNotification("Camera auto-align enabled.");
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowNotification("Camera auto-align disabled.");
                        }
                    }
                }

                if (!CameraAutoAlignEnabled)
                {
                    alignTick = 60;
                    return;
                }

                var camCtrl = MyAPIGateway.Session.CameraController;
                var controller = MyAPIGateway.Session.ControlledObject as Sandbox.Game.Entities.IMyControllableEntity; // Avoiding ambiguity.

                if (camCtrl == null || controller == null)
                    return;

                if (isInFirstPersonView != camCtrl.IsInFirstPersonView)
                {
                    isInFirstPersonView = camCtrl.IsInFirstPersonView;
                    alignTick = 60;
                }

                if (ControlledObject != controller)
                {
                    ControlledObject = controller;
                    alignTick = 60;
                }

                if (MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.LOOKAROUND))
                {
                    alignTick = -1;
                }
                else if (MyAPIGateway.Input.IsGameControlReleased(MyControlsSpace.LOOKAROUND))
                {
                    alignTick = 0;
                }

                if (alignTick != -1)
                {
                    alignTick++;

                    if (alignTick > alignAfter)
                    {
                        alignTick = -1;

                        // Slight change, only allow auto-align on cockpits and characters thrid person.
                        if (controller is IMyRemoteControl)
                        {
                            /*
                            if (RefController == null)
                                return;

                            var shipController = controller as IMyShipController;
                            if (shipController.GetShipSpeed() < 0.1)
                            {
                                alignTick = 0;
                                return;
                            }

                            MyAPIGateway.Utilities.ShowNotification("LULZ");
                            // HACK this is how MyCockpit.Rotate() does things so I kinda have to use these magic numbers.
                            var num = MyAPIGateway.Input.GetMouseSensitivity() * 0.13f;

                            if (camCtrl.IsInFirstPersonView)
                                camCtrl.Rotate(new Vector2(RefController.HeadLocalXAngle / num, RefController.HeadLocalYAngle / num), 0);
                            else
                                camCtrl.Rotate(new Vector2((RefController.HeadLocalXAngle + shipCamHeightOffset) / num, (RefController.HeadLocalYAngle) / num), 0);
                            */
                        }
                        else if (controller is IMyShipController)
                        {
                            //RefController = controller;

                            var shipController = controller as IMyShipController;

                            if (!shipController.CanControlShip)
                            {
                                alignTick = -1;
                                return;
                            }

                            if (shipController.GetShipSpeed() < 0.1)
                            {
                                alignTick = 0;
                                return;
                            }

                            // HACK this is how MyCockpit.Rotate() does things so I kinda have to use these magic numbers.
                            var num = MyAPIGateway.Input.GetMouseSensitivity() * 0.13f;

                            if (camCtrl.IsInFirstPersonView)
                                camCtrl.Rotate(new Vector2(controller.HeadLocalXAngle / num, controller.HeadLocalYAngle / num), 0);
                            else
                                camCtrl.Rotate(new Vector2((controller.HeadLocalXAngle + shipCamHeightOffset) / num, (controller.HeadLocalYAngle) / num), 0);
                        }
                        else if (controller is IMyCharacter)
                        {
                            var playerChar = controller as IMyCharacter;
                            if (playerChar.Physics.Speed < 0.1)
                            {
                                alignTick = 0;
                                return;
                            }

                            // HACK this is how MyCharacter.RotateHead() does things so I kinda have to use these magic numbers.
                            if (!camCtrl.IsInFirstPersonView)
                                camCtrl.Rotate(new Vector2(controller.HeadLocalXAngle * 2, controller.HeadLocalYAngle * 2), 0);
                        }
                        else
                        {
                            /*
                            // HACK this is how MyCharacter.RotateHead() does things so I kinda have to use these magic numbers.
                            if (!camCtrl.IsInFirstPersonView)
                                camCtrl.Rotate(new Vector2(controller.HeadLocalXAngle * 2, controller.HeadLocalYAngle * 2), 0);
                            */
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //MyAPIGateway.Utilities.ShowNotification("NOPE\n" + e.ToString(), 16);
                // I should log. A proper log. Oh well...
            }
        }

        //public override void UpdateBeforeSimulation()
        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible)
                    return;

                CamAlignHandler();
            }
            catch (Exception e)
            {
                //... hm?
            }
        }
    }
}