﻿/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: AGPL-3.0-only
 */
using Markdig.Helpers;

namespace Corsinvest.ProxmoxVE.Admin.Core.Models;

public interface INode
{
    string Node { get; set; }
}