﻿#region Copyright (C) 2008-2009 Simon Allaeys

/*
    Copyright (C) 2008-2009 Simon Allaeys
 
    This file is part of AppStract

    AppStract is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AppStract is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with AppStract.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using AppStract.Core.System.Logging;
using EasyHook;

namespace AppStract.Server.Hooking
{
  /// <summary>
  /// Manages the available hooks, defined as instances of <see cref="HookData"/>.
  /// <see cref="Initialize"/> must be called before <see cref="HookManager"/> can provide any data or install any hook.
  /// </summary>
  public static class HookManager
  {

    #region Variables

    /// <summary>
    /// Whether <see cref="HookManager"/> is initialized.
    /// </summary>
    private static bool _initialized;
    /// <summary>
    /// All hooks that must be installed in the guest process.
    /// </summary>
    private static IList<HookData> _hooks;
    /// <summary>
    /// The hooks that are currently installed in the guest process.
    /// </summary>
    private static IList<LocalHook> _installedHooks;
    /// <summary>
    /// The object to lock when executing actions on any of the global variables.
    /// </summary>
    private static readonly object _syncRoot;

    #endregion

    #region Constructors

    static HookManager()
    {
      _syncRoot = new object();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the manager.
    /// </summary>
    /// <param name="inCallback">An uninterpreted callback that will later be available through <see cref="HookRuntimeInfo.Callback"/>.</param>
    /// <param name="hookHandler">The object containing the methods to associate with hooked functions.</param>
    public static void Initialize(object inCallback, HookImplementations hookHandler)
    {
      lock (_syncRoot)
      {
        if (_initialized)
          return;
        GuestCore.Log(new LogMessage(LogLevel.Debug, "HookManager starts initialization procedure."));
        var hooks = new List<HookData>(8);
        // Hooks regarding the filesystem
        hooks.Add(new HookData("Create Directory [Unicode]",
                               "kernel32.dll", "CreateDirectoryW",
                               new HookDelegates.DCreateDirectory_Unicode(hookHandler.DoCreateDirectory),
                               inCallback));
        hooks.Add(new HookData("Create Directory [Ansi]",
                               "kernel32.dll", "CreateDirectoryA",
                               new HookDelegates.DCreateDirectory_Ansi(hookHandler.DoCreateDirectory),
                               inCallback));
        hooks.Add(new HookData("Create File [Unicode]",
                               "kernel32.dll", "CreateFileW",
                               new HookDelegates.DCreateFile_Unicode(hookHandler.DoCreateFile),
                               inCallback));
        hooks.Add(new HookData("Create File [Ansi]",
                               "kernel32.dll", "CreateFileA",
                               new HookDelegates.DCreateFile_Ansi(hookHandler.DoCreateFile),
                               inCallback));
        hooks.Add(new HookData("Delete File [Unicode]",
                               "kernel32.dll", "DeleteFileW",
                               new HookDelegates.DDeleteFile_Unicode(hookHandler.DoDeleteFile),
                               inCallback));
        hooks.Add(new HookData("Delete File [Ansi]",
                               "kernel32.dll", "DeleteFileA",
                               new HookDelegates.DDeleteFile_Ansi(hookHandler.DoDeleteFile),
                               inCallback));
        hooks.Add(new HookData("Load Library [Unicode]",
                               "kernel32.dll", "LoadLibraryExW",
                               new HookDelegates.DLoadLibraryEx_Unicode(hookHandler.DoLoadLibraryEx),
                               inCallback));
        hooks.Add(new HookData("Load Library [Ansi]",
                               "kernel32.dll", "LoadLibraryExA",
                               new HookDelegates.DLoadLibraryEx_Ansi(hookHandler.DoLoadLibraryEx),
                               inCallback));
        // Hooks regarding the registry
        hooks.Add(new HookData("Open Registry Key [Unicode]",
                               "advapi32.dll", "RegOpenKeyExW",
                               new HookDelegates.DOpenKey_Unicode(hookHandler.RegOpenKey_Hooked),
                               inCallback));
        hooks.Add(new HookData("Open Registry Key [Ansi]",
                               "advapi32.dll", "RegOpenKeyExA",
                               new HookDelegates.DOpenKey_Ansi(hookHandler.RegOpenKey_Hooked),
                               inCallback));
        hooks.Add(new HookData("Create Registry Key [Unicode]",
                               "advapi32.dll", "RegCreateKeyExW",
                               new HookDelegates.DCreateKey_Unicode(hookHandler.RegCreateKeyEx_Hooked),
                               inCallback));
        hooks.Add(new HookData("Create Registry Key [Ansi]",
                               "advapi32.dll", "RegCreateKeyExA",
                               new HookDelegates.DCreateKey_Ansi(hookHandler.RegCreateKeyEx_Hooked),
                               inCallback));
        hooks.Add(new HookData("Close Registry Key",
                               "advapi32.dll", "RegCloseKey",
                               new HookDelegates.DCloseKey(hookHandler.RegCloseKey_Hooked),
                               inCallback));
        hooks.Add(new HookData("Set Registry Value [Unicode]",
                               "advapi32.dll", "RegSetValueExW",
                               new HookDelegates.DSetValue_Unicode(hookHandler.RegSetValueEx),
                               inCallback));
        hooks.Add(new HookData("Set Registry Value [Ansi]",
                               "advapi32.dll", "RegSetValueExA",
                               new HookDelegates.DSetValue_Ansi(hookHandler.RegSetValueEx),
                               inCallback));
        hooks.Add(new HookData("Query Registry Value [Unicode]",
                               "advapi32.dll", "RegQueryValueExW",
                               new HookDelegates.DQueryValue_Unicode(hookHandler.RegQueryValue_Hooked),
                               inCallback));
        hooks.Add(new HookData("Query Registry Value [Ansi]",
                               "advapi32.dll", "RegQueryValueExA",
                               new HookDelegates.DQueryValue_Ansi(hookHandler.RegQueryValue_Hooked),
                               inCallback));
        _hooks = hooks;
        _initialized = true;
        GuestCore.Log(new LogMessage(LogLevel.Debug, "HookManager is initialized."));
      }
    }

    /// <summary>
    /// Installs all available hooks in the local process.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// An <see cref="ApplicationException"/> is thrown if <see cref="Initialize"/> hasn't been called before the current call.
    /// </exception>
    /// <exception cref="HookingException">
    /// A <see cref="HookingException"/> is thrown if the installation of any of the API hooks fails.
    /// </exception>
    public static void InstallHooks()
    {
      GuestCore.Log(new LogMessage(LogLevel.Debug, "HookManager starts installing the API hooks."));
      lock (_syncRoot)
      {
        if (!_initialized)
          throw new ApplicationException("The current instance has not yet been initialized.");
        _installedHooks = new List<LocalHook>();
        foreach (var hook in _hooks)
        {
          try
          {
            var localHook = LocalHook.Create(hook.GetTargetEntryPoint(), hook.Handler, hook.Callback);
            // 0-value in exclusive access control list = don't intercept calls from current thread
            // Bug? Why wouldn't we intercept these?
            localHook.ThreadACL.SetExclusiveACL(new[] {0});
            _installedHooks.Add(localHook);
            GuestCore.Log(new LogMessage(LogLevel.Debug, "HookManager installed API hook: " + hook));
          }
          catch (Exception e)
          {
            GuestCore.Log(
              new LogMessage(LogLevel.Error, "HookManager failed to install API hook: " + hook, e), false);
            throw new HookingException("HookManager failed to install API hook.", hook, e);
          }
        }
      }
    }

    #endregion

  }
}
