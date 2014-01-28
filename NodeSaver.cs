using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/******************************************************************************
 * Copyright (c) 2014, Justin Bengtson
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

namespace RegexKSP {
    public class ModuleNodeSaver : PartModule {
		[KSPField (isPersistant = true)]
		NodeList nodes;

		public ModuleNodeSaver() {
			nodes = new NodeList(this);
		}

		public override void OnInitialize() {
			if(!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null || this.vessel == null) {
				return;
			} else if(this.vessel.patchedConicSolver == null) {
				return;
			}

			PatchedConicSolver p = this.vessel.patchedConicSolver;

			// don't load if we've already got nodes.
			if(p.maneuverNodes.Count > 0) { return; }

			foreach(NodeState n in nodes.nodes) {
				// make sure we have a UT here and that it's in the future
				if(n.UT > Planetarium.GetUniversalTime()) {
					n.createManeuverNode(p);
				}
			}
		}
    }

	[Serializable]
	public class NodeList : IConfigNode {
		private static string[] delimiter = new string[] {","};

		private PartModule parent;
		public List<NodeState> nodes = new List<NodeState>();

		public NodeList(PartModule p) {
			parent = p;
		}
	
		public void Load(ConfigNode node) {
			if(!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null || parent.vessel == null) {
				return;
			}

			string[] values = node.GetValues("node");
			int max = values.Length;
			for(int k = 0; k < max; k++) {
				string[] info = values[k].Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
				Vector3d deltav = new Vector3d();
				double d = 0.0;

				if(info.Length == 4) {
					double.TryParse(info[0], out d);
					deltav.x = d;

					d = 0.0;
					double.TryParse(info[1], out d);
					deltav.y = d;

					d = 0.0;
					double.TryParse(info[2], out d);
					deltav.z = d;

					d = 0.0;
					double.TryParse(info[3], out d);

					// at the very least it'll /act/ like a proper maneuver node.
					nodes.Add(new NodeState(deltav, d));
				}
			}
			Debug.Log("Node Saver loaded " + max + " nodes.");
		}
	
		public void Save(ConfigNode node) {
			if(!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null || parent.vessel == null) {
				return;
			} else if(parent.vessel.patchedConicSolver == null) {
				return;
			}

			// save each node
			foreach(ManeuverNode m in parent.vessel.patchedConicSolver.maneuverNodes) {
				node.AddValue("node", m.DeltaV.x + delimiter[0] + m.DeltaV.y + delimiter[0] + m.DeltaV.z + delimiter[0] + m.UT);
			}
			Debug.Log("Node Saver saved " + parent.vessel.patchedConicSolver.maneuverNodes.Count + " nodes.");
		}
	}
}
