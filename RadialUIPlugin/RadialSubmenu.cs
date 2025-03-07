﻿using UnityEngine;
using BepInEx;
using Bounce.Unmanaged;
using System.Collections.Generic;
using System;

namespace RadialUI
{
    public class RadialSubmenu
    {
        // Radial menu selected asset
        private static CreatureGuid radialAsset = CreatureGuid.Empty;
        private static HideVolumeItem radialHideVolume;

        // Hold sub-entries for main menus
        private static Dictionary<string, List<MapMenu.ItemArgs>> subMenuEntries = new Dictionary<string, List<MapMenu.ItemArgs>>();

        /// <summary>
        /// Enumeration for the type of menu. Hidden volumes are not currently supported since their callback is a little
        /// different but they could be added with a little bit of extra effort
        /// </summary>
        public enum MenuType
        {
            character = 1,
            canAttack,
            cantAttack,
            HideVolume,
        }

        /// <summary>
        /// Method that a plugin uses to ensure that the desired main (radial) menu item exits.
        /// The method creates the entry if it does not exists and ignore the requested if it already exists.
        /// For multiple plugins to share a main menu entry, they need to use the same guid.
        /// </summary>
        /// <param name="mainGuid">Guid for the main menu entry</param>
        /// <param name="type">Determines which type of radial menu the entry is for</param>
        /// <param name="title">Text that is associated with the entry</param>
        /// <param name="icon">Icon that should be displayed</param>
        public static void EnsureMainMenuItem(string mainGuid, MenuType type, string title, Sprite icon)
        {
            // Don't create the main menu entry multiple times
            if (subMenuEntries.ContainsKey(mainGuid)) { return; }
            // Create the main menu entry based on the type of menu
            switch (type)
            {
                case MenuType.character:
                    RadialUI.RadialUIPlugin.AddOnCharacter(mainGuid, new MapMenu.ItemArgs()
                    {
                        // Add mainGuid into the callback so we can look up the corresponding sub-menu entries
                        Action = (mmi, obj) => { DisplaySubmenu(mmi, obj, mainGuid); },
                        Icon = icon,
                        Title = title,
                        CloseMenuOnActivate = false
                    }, Reporter);
                    break;
                case MenuType.canAttack:
                    RadialUI.RadialUIPlugin.AddOnCanAttack(mainGuid, new MapMenu.ItemArgs()
                    {
                        // Add mainGuid into the callback so we can look up the corresponding sub-menu entries
                        Action = (mmi, obj) => { DisplaySubmenu(mmi, obj, mainGuid); },
                        Icon = icon,
                        Title = title,
                        CloseMenuOnActivate = false
                    }, Reporter);
                    break;
                case MenuType.cantAttack:
                    RadialUI.RadialUIPlugin.AddOnCantAttack(mainGuid, new MapMenu.ItemArgs()
                    {
                        // Add mainGuid into the callback so we can look up the corresponding sub-menu entries
                        Action = (mmi, obj) => { DisplaySubmenu(mmi, obj, mainGuid); },
                        Icon = icon,
                        Title = title,
                        CloseMenuOnActivate = false
                    }, Reporter);
                    break;
                case MenuType.HideVolume:
                    RadialUI.RadialUIPlugin.AddOnHideVolume(mainGuid, new MapMenu.ItemArgs()
                    {
                        // Add mainGuid into the callback so we can look up the corresponding sub-menu entries
                        Action = (mmi, obj) => { DisplaySubmenu(mmi, obj, mainGuid); },
                        Icon = icon,
                        Title = title,
                        CloseMenuOnActivate = false
                    }, Reporter);
                    break;
            }
            // Add a list into the dictionary to hold sub-menu entries for the main menu entry
            // (Presence of an entry in this dictionary means the main menu entry has already been created)
            subMenuEntries.Add(mainGuid, new List<MapMenu.ItemArgs>());
        }

        /// <summary>
        /// Add sub-menu items to a maim menu entry
        /// </summary>
        /// <param name="mainGuid">Guid of the main menu entry</param>
        /// <param name="title">Text associated with the sub-menu item</param>
        /// <param name="icon">Icon associated with the sub-menu item</param>
        /// <param name="callback">Callback that is called when the sub-menu item is selected</param>
        public static void CreateSubMenuItem(string mainGuid, string title, Sprite icon, Action<CreatureGuid, string, MapMenuItem> callback, bool closeMenu = true)
        {
            // Check if the main menu Guid exists
            if (!subMenuEntries.ContainsKey(mainGuid))
            {
                Debug.LogWarning("Main radial menu '" + mainGuid + "' does not exits. Use EnsureMainMenuItem() before adding sub-menu items.");
                return;
            }
            // Add the item to the sub-menu item dictionary for the main menu entry (indicated by the Guid)
            subMenuEntries[mainGuid].Add(new MapMenu.ItemArgs()
            {
                // Parent plugin specified callback for when the sub-menu item is selected
                Action = (mmi, obj) => { callback(radialAsset, mainGuid, mmi); },
                Icon = icon,
                Title = title,
                CloseMenuOnActivate = closeMenu
            });
        }

        /// <summary>
        /// Add sub-menu items to a maim menu entry
        /// </summary>
        /// <param name="mainGuid">Guid of the main menu entry</param>
        /// <param name="title">Text associated with the sub-menu item</param>
        /// <param name="icon">Icon associated with the sub-menu item</param>
        /// <param name="callback">Callback that is called when the sub-menu item is selected</param>
        public static void CreateSubMenuItem(string mainGuid, string title, Sprite icon, Action<HideVolumeItem, string, MapMenuItem> callback, bool closeMenu = true)
        {
            // Check if the main menu Guid exists
            if (!subMenuEntries.ContainsKey(mainGuid))
            {
                Debug.LogWarning("Main radial menu '" + mainGuid + "' does not exits. Use EnsureMainMenuItem() before adding sub-menu items.");
                return;
            }
            // Add the item to the sub-menu item dictionary for the main menu entry (indicated by the Guid)
            subMenuEntries[mainGuid].Add(new MapMenu.ItemArgs()
            {
                // Parent plugin specified callback for when the sub-menu item is selected
                Action = (mmi, obj) => { callback(radialHideVolume, mainGuid, mmi); },
                Icon = icon,
                Title = title,
                CloseMenuOnActivate = closeMenu
            });
        }

        /// <summary>
        /// Method for loading icons (sprites) from a file.
        /// </summary>
        /// <param name="fileName">Drive, path and file name of the PNG or JPG file</param>
        /// <returns>Spite holding the icon (which can be passed into the menu creation methods)</returns>
        public static Sprite GetIconFromFile(string fileName)
        {
            Texture2D tex = new Texture2D(32, 32);
            tex.LoadImage(System.IO.File.ReadAllBytes(fileName));
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// mMethid used internally for tracking which mini the radial menu was opened on
        /// </summary>
        /// <param name="selected">Selected mini</param>
        /// <param name="radial">Radial menu mini</param>
        /// <returns></returns>
        private static bool Reporter(NGuid selected, NGuid radial)
        {
            radialAsset = new CreatureGuid(radial);
            return true;
        }

        /// <summary>
        /// mMethid used internally for tracking which mini the radial menu was opened on
        /// </summary>
        /// <param name="item">Radial menu for Hide Volume</param>
        /// <returns></returns>
        private static bool Reporter(HideVolumeItem item)
        {
            radialHideVolume = item;
            return true;
        }

        /// <summary>
        /// Method called by the main menu to generate the corresponding sub-menu
        /// </summary>
        /// <param name="mmi">MapMenuItem associated with the main menu</param>
        /// <param name="obj">Object associated with the main menu</param>
        /// <param name="mainGuid">Guid of the main menu</param>
        private static void DisplaySubmenu(MapMenuItem mmi, object obj, string mainGuid)
        {
            // Create sub-menu
            MapMenu mapMenu = MapMenuManager.OpenMenu(mmi, MapMenu.MenuType.BRANCH);
            // Populate sub-menu based on all items added by any plugins for the specific main menu entry
            foreach (MapMenu.ItemArgs item in subMenuEntries[mainGuid])
            {
                mapMenu.AddItem(item);
            }
        }
    }
}
