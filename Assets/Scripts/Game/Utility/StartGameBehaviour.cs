﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2016 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Lypyl (lypyldf@gmail.com)
// 
// Notes:
//

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallConnect.Save;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Questing;

namespace DaggerfallWorkshop.Game.Utility
{
    /// <summary>
    /// Game startup and shutdown helper.
    /// </summary>
    public class StartGameBehaviour : MonoBehaviour
    {
        #region Fields

        // Editor properties
        public StartMethods StartMethod = StartMethods.DoNothing;
        public int SaveIndex = -1;
        public string PostStartMessage = string.Empty;
        public string LaunchQuest = string.Empty;
        public bool EnableVideos = true;
        public bool ShowEditorFlats = false;
        public bool NoWorld = false;
        public bool GodMode = false;

        //events used to update state in state manager
        public static System.EventHandler OnStartMenu;
        public static System.EventHandler OnStartGame;

        // Private fields
        CharacterDocument characterDocument;
        int classicSaveIndex = -1;
        GameObject player;
        PlayerEnterExit playerEnterExit;
        StartMethods lastStartMethod;

        #endregion

        #region Properties

        public CharacterDocument CharacterDocument
        {
            get { return characterDocument; }
            set { characterDocument = value; }
        }

        public int ClassicSaveIndex
        {
            get { return classicSaveIndex; }
            set { classicSaveIndex = value; }
        }

        public StartMethods LastStartMethod
        {
            get { return lastStartMethod; }
        }

        #endregion

        #region Enums

        public enum StartMethods
        {
            DoNothing,                              // No startup action
            Void,                                   // Start to the Void
            TitleMenu,                              // Open title menu
            TitleMenuFromDeath,                     // Open title menu after death
            NewCharacter,                           // Spawn character to start location in INI
            LoadDaggerfallUnitySave,                // Make this work with new save/load system
            LoadClassicSave,                        // Loads a classic save using start save index
        }

        #endregion

        #region Unity

        void Awake()
        {
            // Get player objects
            player = FindPlayer();
            playerEnterExit = FindPlayerEnterExit(player);
        }

        void Start()
        {
            ApplyStartSettings();
            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;
        }

        void Update()
        {
            // Restart game using method provided
            if (StartMethod != StartMethods.DoNothing)
            {
                GameManager.Instance.PauseGame(true);
                InvokeStartMethod();
                StartMethod = StartMethods.DoNothing;
            }
        }

        #endregion

        #region Private Methods

        void InvokeStartMethod()
        {
            switch (StartMethod)
            {
                case StartMethods.Void:
                    StartVoid();
                    break;
                case StartMethods.TitleMenu:
                    StartTitleMenu();
                    break;
                case StartMethods.TitleMenuFromDeath:
                    StartTitleMenuFromDeath();
                    break;
                case StartMethods.NewCharacter:
                    StartNewCharacter();
                    break;
                case StartMethods.LoadDaggerfallUnitySave:
                    LoadDaggerfallUnitySave();
                    break;
                case StartMethods.LoadClassicSave:
                    if (SaveIndex != -1) classicSaveIndex = SaveIndex;
                    StartFromClassicSave();
                    break;
                default:
                    break;
            }

            // Reset save index
            SaveIndex = -1;
        }

        #endregion

        #region Common Startup

        void ApplyStartSettings()
        {
            // Resolution
            Screen.SetResolution(
                DaggerfallUnity.Settings.ResolutionWidth,
                DaggerfallUnity.Settings.ResolutionHeight,
                DaggerfallUnity.Settings.Fullscreen);

            // Camera settings
            GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObject)
            {
                // Set camera FOV
                Camera camera = cameraObject.GetComponent<Camera>();
                if (camera)
                    camera.fieldOfView = DaggerfallUnity.Settings.FieldOfView;

                // Set mouse look
                PlayerMouseLook mouseLook = cameraObject.GetComponent<PlayerMouseLook>();
                if (mouseLook)
                    mouseLook.invertMouseY = DaggerfallUnity.Settings.InvertMouseVertical;

                // Set mouse look smoothing
                if (mouseLook)
                    mouseLook.enableSmoothing = DaggerfallUnity.Settings.MouseLookSmoothing;

                // Set mouse look sensitivity
                if (mouseLook)
                    mouseLook.sensitivityScale = DaggerfallUnity.Settings.MouseLookSensitivity;

                // Set rendering path
                if (DaggerfallUnity.Settings.UseLegacyDeferred)
                    camera.renderingPath = RenderingPath.DeferredLighting;
            }

            // Shadow Resoltion Mode
            switch (DaggerfallUnity.Settings.ShadowResolutionMode)
            {
                case 0:
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    break;
                case 1:
                default:
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    break;
                case 2:
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
                case 3:
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    break;
            }        

            // VSync settings
            if (DaggerfallUnity.Settings.VSync)
                QualitySettings.vSyncCount = 1;
            else
                QualitySettings.vSyncCount = 0;

            // Filter settings
            DaggerfallUnity.Instance.MaterialReader.MainFilterMode = (FilterMode)DaggerfallUnity.Settings.MainFilterMode;

            // HUD settings
            DaggerfallHUD hud = DaggerfallUI.Instance.DaggerfallHUD;
            if (hud != null)                                              //null at startup
                hud.ShowCrosshair = DaggerfallUnity.Settings.Crosshair; 

            // Weapon swing settings
            WeaponManager weaponManager = GameManager.Instance.WeaponManager;
            weaponManager.AttackThreshold = DaggerfallUnity.Settings.WeaponAttackThreshold;

            // Weapon hand settings
            // Only supporting left-hand rendering for now
            // More handedness options may be added later
            if (DaggerfallUnity.Settings.Handedness == 1)
                weaponManager.RightHandWeapon.LeftHand = true;

            // GodMode setting
            PlayerEntity playerEntity = FindPlayerEntity();
            playerEntity.GodMode = GodMode;

            // Enable/disable videos
            DaggerfallUI.Instance.enableVideos = EnableVideos;
        }

        #endregion

        #region Startup Methods

        void StartVoid()
        {
            RaiseOnNewGameEvent();
            DaggerfallUI.Instance.PopToHUD();
            playerEnterExit.DisableAllParents();
            ResetWeaponManager();
            NoWorld = true;
            lastStartMethod = StartMethods.Void;
        }

        void StartTitleMenu()
        {
            RaiseOnNewGameEvent();
            DaggerfallUI.Instance.PopToHUD();
            playerEnterExit.DisableAllParents();
            ResetWeaponManager();

            if (string.IsNullOrEmpty(PostStartMessage))
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiInitGame);
            else
                DaggerfallUI.PostMessage(PostStartMessage);

            lastStartMethod = StartMethods.TitleMenu;

            if (OnStartMenu != null)
                OnStartMenu(this, null);
        }

        void StartTitleMenuFromDeath()
        {
            RaiseOnNewGameEvent();

            // Reset player death camera
            if (GameManager.Instance.PlayerDeath)
                GameManager.Instance.PlayerDeath.ResetCamera();

            // Set behaviour back to title menu
            StartMethod = StartMethods.TitleMenu;

            DaggerfallUI.Instance.PopToHUD();
            playerEnterExit.DisableAllParents();
            ResetWeaponManager();

            if (string.IsNullOrEmpty(PostStartMessage))
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiInitGameFromDeath);
            else
                DaggerfallUI.PostMessage(PostStartMessage);

            lastStartMethod = StartMethods.TitleMenuFromDeath;

            if (OnStartMenu != null)
                OnStartMenu(this, null);
        }

        // Start new character to location specified in INI
        void StartNewCharacter()
        {
            DaggerfallUnity.ResetUID();
            RaiseOnNewGameEvent();
            DaggerfallUI.Instance.PopToHUD();
            ResetWeaponManager();

            // Must have a character document
            if (characterDocument == null)
                characterDocument = new CharacterDocument();

            // Assign character sheet
            PlayerEntity playerEntity = FindPlayerEntity();
            playerEntity.AssignCharacter(characterDocument);

            // Set game time
            DaggerfallUnity.Instance.WorldTime.Now.SetClassicGameStartTime();

            // Get start parameters
            DFPosition mapPixel = new DFPosition(DaggerfallUnity.Settings.StartCellX, DaggerfallUnity.Settings.StartCellY);
            bool startInDungeon = DaggerfallUnity.Settings.StartInDungeon;

            // Read location if any
            DFLocation location = new DFLocation();
            ContentReader.MapSummary mapSummary;
            bool hasLocation = DaggerfallUnity.Instance.ContentReader.HasLocation(mapPixel.X, mapPixel.Y, out mapSummary);
            if (hasLocation)
            {
                if (!DaggerfallUnity.Instance.ContentReader.GetLocation(mapSummary.RegionIndex, mapSummary.MapIndex, out location))
                    hasLocation = false;
            }

            if (NoWorld)
            {
                playerEnterExit.DisableAllParents();
            }
            else
            {
                // Start at specified location
                StreamingWorld streamingWorld = FindStreamingWorld();
                if (hasLocation && startInDungeon && location.HasDungeon)
                {
                    if (streamingWorld)
                    {
                        streamingWorld.TeleportToCoordinates(mapPixel.X, mapPixel.Y);
                        streamingWorld.suppressWorld = true;
                    }
                    playerEnterExit.EnableDungeonParent();
                    playerEnterExit.StartDungeonInterior(location);
                }
                else
                {
                    playerEnterExit.EnableExteriorParent();
                    if (streamingWorld)
                    {
                        streamingWorld.SetAutoReposition(StreamingWorld.RepositionMethods.Origin, Vector3.zero);
                        streamingWorld.suppressWorld = false;
                    }
                }
            }

            // Assign starting gear to player entity
            DaggerfallUnity.Instance.ItemHelper.AssignStartingGear(playerEntity);

            //##Setup bank accounts
            Banking.DaggerfallBankManager.SetupAccounts();

            // Start game
            GameManager.Instance.PauseGame(false);
            DaggerfallUI.Instance.FadeHUDFromBlack();
            DaggerfallUI.PostMessage(PostStartMessage);

            lastStartMethod = StartMethods.NewCharacter;

            // Launch quest
            if (!string.IsNullOrEmpty(LaunchQuest))
            {
                QuestMachine.Instance.InstantiateQuest(LaunchQuest);
                LaunchQuest = string.Empty;
            }

            if (OnStartGame != null)
                OnStartGame(this, null);
        }

        #endregion

        #region Daggerfall Unity Save Startup

        void LoadDaggerfallUnitySave()
        {
            if (SaveIndex == -1)
                return;

            SaveLoadManager.Instance.EnumerateSaves();
            SaveLoadManager.Instance.Load(SaveIndex);
        }

        #endregion

        #region Classic Save Startup

        void StartFromClassicSave()
        {
            DaggerfallUnity.ResetUID();
            RaiseOnNewGameEvent();
            ResetWeaponManager();

            // Save index must be in range
            if (classicSaveIndex < 0 || classicSaveIndex >= 6)
                throw new IndexOutOfRangeException("classicSaveIndex out of range.");

            // Open saves in parent path of Arena2 folder
            string path = SaveLoadManager.Instance.DaggerfallSavePath;
            SaveGames saveGames = new SaveGames(path);
            if (!saveGames.IsPathOpen)
                throw new Exception(string.Format("Could not open Daggerfall saves path {0}", path));

            // Open save index
            if (!saveGames.TryOpenSave(classicSaveIndex))
            {
                string error = string.Format("Could not open classic save index {0}.", classicSaveIndex);
                DaggerfallUI.MessageBox(error);
                DaggerfallUnity.LogMessage(string.Format(error), true);
                return;
            }

            // Get required save data
            SaveTree saveTree = saveGames.SaveTree;
            SaveVars saveVars = saveGames.SaveVars;

            if (NoWorld)
            {
                playerEnterExit.DisableAllParents();
            }
            else
            {
                // Set player to world position
                playerEnterExit.EnableExteriorParent();
                StreamingWorld streamingWorld = FindStreamingWorld();
                int worldX = saveTree.Header.CharacterPosition.Position.WorldX;
                int worldZ = saveTree.Header.CharacterPosition.Position.WorldZ;
                streamingWorld.TeleportToWorldCoordinates(worldX, worldZ);
                streamingWorld.suppressWorld = false;
            }

            GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            PlayerMouseLook mouseLook = cameraObject.GetComponent<PlayerMouseLook>();
            if (mouseLook)
            {
                // Classic save value ranges from -256 (looking up) to 256 (looking down).
                // The maximum up and down range of view in classic is similar to 45 degrees up and down in DF Unity.
                float pitch = saveTree.DirectionRecord.Pitch;
                if (pitch != 0)
                    pitch = (pitch * 45 / 256);
                mouseLook.Pitch = pitch;

                float yaw = saveTree.DirectionRecord.Yaw;
                // In classic saves 2048 units of yaw is 360 degrees.
                if (yaw != 0)
                    yaw = (yaw * 360 / 2048);
                mouseLook.Yaw = yaw;
            }

            // Set whether the player's weapon is drawn
            GameManager.Instance.WeaponManager.Sheathed = (!saveVars.WeaponDrawn);

            // Set game time
            DaggerfallUnity.Instance.WorldTime.Now.FromClassicDaggerfallTime(saveVars.GameTime);

            // Get character record
            List<SaveTreeBaseRecord> records = saveTree.FindRecords(RecordTypes.Character);
            if (records.Count != 1)
                throw new Exception("SaveTree CharacterRecord not found.");

            // Get prototypical character document data
            CharacterRecord characterRecord = (CharacterRecord)records[0];
            characterDocument = characterRecord.ToCharacterDocument();

            // Assign data to player entity
            PlayerEntity playerEntity = FindPlayerEntity();
            playerEntity.AssignCharacter(characterDocument, characterRecord.ParsedData.level, characterRecord.ParsedData.maxHealth, false);

            // Set time of last check for raising skills
            playerEntity.TimeOfLastSkillIncreaseCheck = saveVars.LastSkillCheckTime;

            // Assign items to player entity
            playerEntity.AssignItems(saveTree);

            // Assign gold pieces
            playerEntity.GoldPieces = (int)characterRecord.ParsedData.physicalGold;

            // GodMode setting
            playerEntity.GodMode = saveVars.GodMode;

            // Setup bank accounts
            var bankRecords = saveTree.FindRecord(RecordTypes.BankAccount);
            Banking.DaggerfallBankManager.ReadNativeBankData(bankRecords);

            // Start game
            DaggerfallUI.Instance.PopToHUD();
            GameManager.Instance.PauseGame(false);
            DaggerfallUI.Instance.FadeHUDFromBlack();
            DaggerfallUI.PostMessage(PostStartMessage);

            lastStartMethod = StartMethods.LoadClassicSave;
            SaveIndex = -1;

            if (OnStartGame != null)
                OnStartGame(this, null);
        }

        #endregion

        #region Utility

        StreamingWorld FindStreamingWorld()
        {
            StreamingWorld streamingWorld = GameObject.FindObjectOfType<StreamingWorld>();
            if (!streamingWorld)
                throw new Exception("Could not find StreamingWorld.");

            return streamingWorld;
        }

        GameObject FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (!player)
                throw new Exception("Could not find Player.");

            return player;
        }

        PlayerEnterExit FindPlayerEnterExit(GameObject player)
        {
            PlayerEnterExit playerEnterExit = player.GetComponent<PlayerEnterExit>();
            if (!playerEnterExit)
                throw new Exception("Could not find PlayerEnterExit.");

            return playerEnterExit;
        }

        PlayerEntity FindPlayerEntity()
        {
            GameObject player = FindPlayer();
            PlayerEntity playerEntity = player.GetComponent<DaggerfallEntityBehaviour>().Entity as PlayerEntity;

            return playerEntity;
        }

        void ResetWeaponManager()
        {
            // Weapon hand and equip state not serialized currently
            // Interim measure is to reset weapon manager state on new game
            GameManager.Instance.WeaponManager.Reset();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Launch starting quest on first load.
        /// This is only used as a debug helper to launch a quest after
        /// automatically loading a DFUnity save game.
        /// </summary>
        private void SaveLoadManager_OnLoad(SaveData_v1 saveData)
        {
            if (!string.IsNullOrEmpty(LaunchQuest))
            {
                QuestMachine.Instance.InstantiateQuest(LaunchQuest);
                LaunchQuest = string.Empty;
            }
        }

        #endregion

        #region Events

        // OnNewGame
        public delegate void OnNewGameEventHandler();
        public static event OnNewGameEventHandler OnNewGame;
        protected virtual void RaiseOnNewGameEvent()
        {
            if (OnNewGame != null)
                OnNewGame();
        }

        #endregion
    }
}
