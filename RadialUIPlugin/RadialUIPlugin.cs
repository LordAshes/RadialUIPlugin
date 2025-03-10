﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx;
using Bounce.Unmanaged;
using Object = System.Object;

namespace RadialUI
{

    [BepInPlugin(Guid, "RadialUIPlugin", Version)]
    public class RadialUIPlugin : BaseUnityPlugin
    {
        // constants
        public const string Guid = "org.hollofox.plugins.RadialUIPlugin";
        private const string Version = "1.2.0.0";

        /// <summary>
        /// Awake plugin
        /// </summary>
        void Awake()
        {
            Logger.LogInfo("In Awake for RadialUI");

            Debug.Log("RadialUI Plug-in loaded");
        }


        // Character Related
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<NGuid,NGuid, bool>)> _onCharacterCallback = new Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)>();
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)> _onCanAttack = new Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)>();
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)> _onCantAttack = new Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)>();
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)> _onSubmenuEmotes = new Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)>();
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)> _onSubmenuKill = new Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)>();
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)> _onSubmenuGm = new Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)>();
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)> _onSubmenuAttacks = new Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)>();

        // Hide Volumes
        private static readonly Dictionary<string, (MapMenu.ItemArgs, Func<HideVolumeItem,bool>)> _onHideVolumeCallback = new Dictionary<string, (MapMenu.ItemArgs, Func<HideVolumeItem,bool>)>();

        // Add On Character
        public static void AddOnCharacter(string key, MapMenu.ItemArgs value, Func<NGuid, NGuid, bool> externalCheck = null) => _onCharacterCallback.Add(key,(value, externalCheck));
        public static void AddOnCanAttack(string key, MapMenu.ItemArgs value, Func<NGuid, NGuid, bool> externalCheck = null) => _onCanAttack.Add(key,(value, externalCheck));
        public static void AddOnCantAttack(string key, MapMenu.ItemArgs value, Func<NGuid, NGuid, bool> externalCheck = null) => _onCantAttack.Add(key,(value, externalCheck));
        public static void AddOnSubmenuEmotes(string key, MapMenu.ItemArgs value, Func<NGuid, NGuid, bool> externalCheck = null) => _onSubmenuEmotes.Add(key,(value, externalCheck));
        public static void AddOnSubmenuKill(string key, MapMenu.ItemArgs value, Func<NGuid, NGuid, bool> externalCheck = null) => _onSubmenuKill.Add(key,(value, externalCheck));
        public static void AddOnSubmenuGm(string key, MapMenu.ItemArgs value, Func<NGuid, NGuid, bool> externalCheck = null) => _onSubmenuGm.Add(key,(value, externalCheck));
        public static void AddOnSubmenuAttacks(string key, MapMenu.ItemArgs value, Func<NGuid, NGuid, bool> externalCheck = null) => _onSubmenuAttacks.Add(key,(value, externalCheck));

        // Add On HideVolume
        public static void AddOnHideVolume(string key, MapMenu.ItemArgs value, Func<HideVolumeItem,bool> externalCheck = null) => _onHideVolumeCallback.Add(key, (value, externalCheck));

        // Remove On Character
        public static bool RemoveOnCharacter(string key) => _onCharacterCallback.Remove(key);
        public static bool RemoveOnCanAttack(string key) => _onCanAttack.Remove(key);
        public static bool RemoveOnCantAttack(string key) => _onCantAttack.Remove(key);
        public static bool RemoveOnSubmenuEmotes(string key) => _onSubmenuEmotes.Remove(key);
        public static bool RemoveOnSubmenuKill(string key) => _onSubmenuKill.Remove(key);
        public static bool RemoveOnSubmenuGm(string key) => _onSubmenuGm.Remove(key);
        public static bool RemoveOnSubmenuAttacks(string key) => _onSubmenuAttacks.Remove(key);

        // Remove On HideVolume
        public static bool RemoveOnHideVolume(string key) => _onHideVolumeCallback.Remove(key);

        // Check to see if map menu is new
        private static int last = 0;
        private static string lastTitle = "";
        private static bool lastSuccess = true;

        /// <summary>
        /// Looping method run by plugin
        /// </summary>
        void Update()
        {
            if (MapMenuManager.HasInstance && MapMenuManager.IsOpen)
            {

                var instance = MapMenuManager.Instance;
                var type = instance.GetType();

                var field = type.GetField("_allOpenMenus", bindFlags);
                var menus = (List<MapMenu>)field.GetValue(instance);

                if (menus.Count >= 1 && menus.Count >= last)
                {
                    try
                    {
                        var map = menus[menus.Count - 1];
                        var Map = map.transform.GetChild(0).GetChild(0);
                        var mapComponent = Map.GetComponent<MapMenuItem>();
                        var mapField = mapComponent.GetType().GetField("_title", bindFlags);
                        var title = (string) mapField.GetValue(mapComponent);
                        if (menus.Count == last && lastTitle != title) last -= 1;
                        Probe();
                        lastTitle = title;
                        lastSuccess = true;
                    }
                    catch (Exception e)
                    {
                        // Probably Stat open
                        if (lastSuccess == true)
                        {
                            Debug.Log($"Error: {e}, most likely stat submenu being opened");
                            lastSuccess = false;
                        }
                    }
                }
                last = menus.Count;
            }
            else
            {
                last = 0;
                lastTitle = "";
                lastSuccess = true;
            }
        }

        private const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private void Probe()
        { 
            var instance = MapMenuManager.Instance;
            var type = instance.GetType();
            
            var field = type.GetField("_allOpenMenus", bindFlags);
            var menus = (List<MapMenu>) field.GetValue(instance);

            for(var i = last; i < menus.Count; i++)
            {
                var map = menus[i];
                var Map = map.transform.GetChild(0).GetChild(0);
                var mapComponent = Map.GetComponent<MapMenuItem>();

                var mapField = mapComponent.GetType().GetField("_title", bindFlags);
                var title = (string)mapField.GetValue(mapComponent);

                var id = LocalClient.SelectedCreatureId.Value;

                // Minis Related
                if (IsMini(title)) AddCreatureEvent(_onCharacterCallback,id,map);
                if (CanAttack(title)) AddCreatureEvent(_onCanAttack, id, map);
                if (CanNotAttack(title)) AddCreatureEvent(_onCantAttack, id, map);
                
                // Minis Submenu
                if (IsEmotes(title)) AddCreatureEvent(_onSubmenuEmotes, id, map);
                if (IsKill(title)) AddCreatureEvent(_onSubmenuKill, id, map);
                if (IsGmMenu(title)) AddCreatureEvent(_onSubmenuGm, id, map);
                if (IsAttacksMenu(title)) AddCreatureEvent(_onSubmenuAttacks, id, map);

                // Hide Volumes
                if (IsHideVolume(title)) AddHideVolumeEvent(_onHideVolumeCallback, map);
            }
        }

        private void AddCreatureEvent(Dictionary<string, (MapMenu.ItemArgs, Func<NGuid, NGuid, bool>)> dic, NGuid myCreature, MapMenu map)
        {
            var target = GetRadialTargetCreature();
            foreach (var handlers
                in dic.Values
                    .Where(handlers => handlers.Item2 == null
                                       || handlers.Item2(myCreature, target))) map.AddItem(handlers.Item1);
        }

        public static NGuid GetRadialTargetCreature()
        {
            var x = (CreatureMenuBoardTool)GameObject.FindObjectOfType(typeof(CreatureMenuBoardTool));

            FieldInfo mapField = x.GetType().GetField("_selectedCreature", bindFlags);
            var selectedCreature = (Creature)mapField.GetValue(x);
            return selectedCreature.CreatureId.Value;
        }

        private void AddHideVolumeEvent(Dictionary<string, (MapMenu.ItemArgs, Func<HideVolumeItem, bool>)> dic, MapMenu map)
        {
            var target = GetSelectedHideVolumeItem();
            foreach (var handlers
                in dic.Values
                    .Where(handlers => handlers.Item2 == null
                                       || handlers.Item2(target))) map.AddItem(handlers.Item1);
        }

        private static HideVolumeItem GetSelectedHideVolumeItem()
        {
            var tool = (GMHideVolumeMenuBoardTool)GameObject.FindObjectOfType(typeof(GMHideVolumeMenuBoardTool));
            FieldInfo mapField = tool.GetType().GetField("_selectedVolume", bindFlags);
            return (HideVolumeItem) mapField.GetValue(tool);
        }


        // Current ShortHand to see if Mini
        private bool IsMini(string title) => title == "Emotes" || title == "Attacks";
        private bool CanAttack(string title) => title == "Attacks";
        private bool CanNotAttack(string title) => title == "Emotes";

        // Mini Submenus
        private bool IsEmotes(string title) => title == "Twirl";
        private bool IsKill(string title) => title == "Kill Creature";
        private bool IsGmMenu(string title) => title == "Player Permission";
        private bool IsAttacksMenu(string title) => title == "Attack";

        // Current ShortHand to see if HideVolume
        private bool IsHideVolume(string title) => title == "Toggle Visibility";
    }
}
