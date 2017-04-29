﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.CorDebug.UI;

namespace dnSpy.Debugger.CorDebug.Code {
	abstract class BreakpointFormatterService {
		public abstract DbgBreakpointLocationFormatterImpl Create(DbgDotNetNativeCodeLocation location);
	}

	[Export(typeof(BreakpointFormatterService))]
	sealed class BreakpointFormatterServiceImpl : BreakpointFormatterService {
		readonly Lazy<IDecompilerService> decompilerService;
		readonly DbgCodeBreakpointDisplaySettings dbgCodeBreakpointDisplaySettings;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		internal IDecompiler MethodDecompiler => decompilerService.Value.Decompiler;

		[ImportingConstructor]
		BreakpointFormatterServiceImpl(UIDispatcher uiDispatcher, Lazy<IDecompilerService> decompilerService, DbgCodeBreakpointDisplaySettings dbgCodeBreakpointDisplaySettings, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgMetadataService> dbgMetadataService) {
			this.decompilerService = decompilerService;
			this.dbgCodeBreakpointDisplaySettings = dbgCodeBreakpointDisplaySettings;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgMetadataService = dbgMetadataService;
			uiDispatcher.UI(() => decompilerService.Value.DecompilerChanged += DecompilerService_DecompilerChanged);
		}

		void DecompilerService_DecompilerChanged(object sender, EventArgs e) {
			foreach (var bp in dbgCodeBreakpointsService.Value.Breakpoints) {
				if (bp.IsHidden)
					continue;
				var formatter = (bp.Location as DbgDotNetNativeCodeLocationImpl)?.Formatter;
				formatter?.RefreshName();
			}
		}

		public override DbgBreakpointLocationFormatterImpl Create(DbgDotNetNativeCodeLocation location) =>
			new DbgBreakpointLocationFormatterImpl(this, dbgCodeBreakpointDisplaySettings, (DbgDotNetNativeCodeLocationImpl)location);

		internal TDef GetDefinition<TDef>(ModuleId module, uint token) where TDef : class {
			var md = dbgMetadataService.Value.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
			return md?.ResolveToken(token) as TDef;
		}
	}
}
