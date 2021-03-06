﻿#region Copyright (C) 2009-2010 Simon Allaeys

/*
    Copyright (C) 2009-2010 Simon Allaeys
 
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

namespace AppStract.Utilities.ManagedFusion.Insuring
{
  /// <summary>
  /// Specifies the additional means of protection for the local global assembly cache.
  /// </summary>
  [Flags]
  public enum CleanUpInsuranceFlags
  {
    /// <summary>
    /// Don't use any additional means of protection.
    /// </summary>
    None = 0x00,
    /// <summary>
    /// Keep track of installed assemblies by saving them in a file until they are uninstalled.
    /// </summary>
    TrackByFile = 0x01,
    /// <summary>
    /// Keep track of installed assemblies by saving them in the local registry until they are uninstalled.
    /// </summary>
    TrackByRegistry = 0x02,
    /// <summary>
    /// Use a third process to ensure clean up of the global assembly cache.
    /// </summary>
    ByWatchService = 0x04,
    /// <summary>
    /// Use all available methods for ensuring a clean global assembly cache.
    /// </summary>
    All = TrackByFile | TrackByRegistry | ByWatchService
  }

  /// <summary>
  /// Provides extension methods for <see cref="CleanUpInsuranceFlags"/>.
  /// </summary>
  public static class  CleanUpInsuranceFlagsExtensions
  {
    /// <summary>
    /// Returns whether the <see cref="CleanUpInsuranceFlags"/> specified is/are defined in the current <see cref="CleanUpInsuranceFlags"/>.
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="flag"></param>
    /// <returns></returns>
    public static bool IsSpecified(this CleanUpInsuranceFlags flags, CleanUpInsuranceFlags flag)
    {
      return ((flags & flag) == flag);
    }
  }

}
