/*
This file ist part of a MuID research.

Licenses under GPL v2 if not stated otherwise.
We care about Open Source. Contributions & feedback welcome.

Please visit us at https://prototyping.muid.sh

*/

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using VVVV.PluginInterfaces.V2;
using VVVV.Core;
using VVVV.Core.Logging;
using System.ComponentModel.Composition;

using MuID.Utils;

namespace VVVV.Nodes {
	
	[PluginInfo(Name = "RingStream", Category = "Raw", Help = "Implements a Ringbuffer as a Stream", Tags = "")]
	public class RingStreamNode : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
		
		[Input("Input")]
		public IDiffSpread<Stream> Input;
		
		[Input("Buffer Size")]
		public IDiffSpread<int> BufferSizes;
		
		[Output("Output")]
		public ISpread<Stream> Output;
		
		[Output("Buffer Object IDs")]
		public ISpread<int> FObjectIds;
		
		[Output("Debug")]
		public ISpread<string> Debug;
		
		[Import()]
		ILogger Logger;
		
		public void OnImportsSatisfied() {
			Output.SliceCount = 0;
			Output.ResizeAndDispose(0,(int i) => new RingStream(BufferSizes[i]));
		}
		
		public void Dispose() {
			foreach(RingStream rs in Output) rs.Dispose();
		}
		
		int lastSliceCount = 0;
		public void Evaluate(int SpreadMax) {
			try {
				if (lastSliceCount != Input.SliceCount) {
					Output.ResizeAndDispose(Input.SliceCount,(int i) => new RingStream(BufferSizes[i]));	
				}
				lastSliceCount = Input.SliceCount;
				FObjectIds.SliceCount = Output.SliceCount;
				Debug.SliceCount = 0;
				for(int i=0; i<Output.SliceCount; i++) {
					var buff = Output[i] as RingStream;
					if (BufferSizes.IsChanged) {
						buff.BufferSize = BufferSizes[i];
					}
					if(Input.IsChanged) {
						var inStream = Input[i];
						if (inStream != null) {
							buff.CopyFrom(inStream);
						}
					}
					Output[i] = buff;
					FObjectIds[i] = Output[i].GetHashCode();
					Debug.Add(buff.ToString());
				}
				
			} catch (Exception e) {
				Logger.Log(e);
			}
		}
	}
}