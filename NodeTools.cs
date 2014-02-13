using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
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

	public class NodeTools {
		/// <summary>
		/// Sets the conics render mode
		/// </summary>
		/// <param name="mode">The conics render mode to use, one of 0, 1, 2, 3, or 4.  Arguments outside those will be set to 3.</param>
		public static void changeConicsMode(int mode) {
			switch(mode) {
				case 0:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_TO_BODIES;
					break;
				case 1:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_AT_SOI_ENTRY_UT;
					break;
				case 2:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_AT_SOI_EXIT_UT;
					break;
				case 3:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.RELATIVE;
					break;
				case 4:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.DYNAMIC;
					break;
				default:
					// revert to KSP default
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.RELATIVE;
					break;
			}
		}

		/// <summary>
		/// Creates a new Meneuver Node Gizmo if needed
		/// </summary>
		public static void CreateNodeGizmo(ManeuverNode node) {
			if(node.attachedGizmo != null) { return; }
			PatchRendering pr = FlightGlobals.ActiveVessel.patchedConicRenderer.FindRenderingForPatch(node.patch);
			node.AttachGizmo(MapView.ManeuverNodePrefab, FlightGlobals.ActiveVessel.patchedConicRenderer, pr);
		}

		/// <summary>
		/// Converts the UT to human-readable Kerbal local time.
		/// </summary>
		/// <returns>The converted time.</returns>
		/// <param name="UT">Kerbal Spece Program Universal Time.</param>
		public static String convertUTtoHumanTime(double UT) {
			long secs = (long)Math.Floor(UT % 60);
			long mins = (long)Math.Floor((UT / 60) % 60);
			long hour = (long)Math.Floor((UT / 3600) % 24);
			long day = (long)Math.Floor((UT / 86400) % 365) + 1;  // Ensure we don't get a "Day 0" here.
			long year = (long)Math.Floor(UT / (86400 * 365)) + 1; // Ensure we don't get a "Year 0" here.

			return "Year " + year + " Day " + day + " " + hour + ":" + (mins < 10 ? "0" : "") + mins + ":" + (secs < 10 ? "0" : "") + secs;
		}

		/// <summary>
		/// Converts the UT to human-readable duration.
		/// </summary>
		/// <returns>The converted time.</returns>
		/// <param name="UT">Kerbal Spece Program Universal Time.</param>
		public static String convertUTtoHumanDuration(double UT) {
			double temp = Math.Floor(Math.Abs(UT % 60));
			string retval = (long)temp + "s";
			if(Math.Abs(UT / 60) > 1.0) {
				temp = Math.Floor(Math.Abs((UT / 60) % 60));
				retval = (long)temp + "m, " + retval;
			}
			if(Math.Abs(UT / 3600) > 1.0) {
				temp = Math.Floor(Math.Abs((UT / 3600) % 24));
				retval = (long)temp + "h, " + retval;
			}
			if(Math.Abs(UT / 86400) > 1.0) {
				temp = Math.Floor(Math.Abs((UT / 86400) % 365));
				retval = ((long)temp + 1) + "d, " + retval;
			}
			if(Math.Abs(UT / (86400 * 365)) > 1.0) {
				temp = Math.Floor(Math.Abs(UT / (86400 * 365)));
				retval = ((long)temp + 1) + "y, " + retval;
			}
			return retval;
		}

		/// <summary>
		/// Merges the given node into the next lowest node (n's index - 1).  If there is no lower node, does nothing.
		/// </summary>
		/// <param name="n">The ManeuverNode to merge down.</param>
		public static void mergeNodeDown(ManeuverNode n) {
			PatchedConicSolver p = NodeTools.getSolver();
			Orbit o = FlightGlobals.ActiveVessel.orbit;
			int nodes = p.maneuverNodes.Count;
			int idx = p.maneuverNodes.IndexOf(n);

			// if we're the last or only node, don't bother.
			if(idx == 0 || nodes < 2) { return; }
			ManeuverNode mergeInto = p.maneuverNodes[idx-1];

			Vector3d deltaV = mergeBurnVectors(mergeInto.UT, mergeInto, n.patch);

			mergeInto.OnGizmoUpdated(deltaV, mergeInto.UT);
			p.maneuverNodes.Remove(n);
		}

		// calculation function for mergeNodeDown
		private static Vector3d mergeBurnVectors(double UT, ManeuverNode first, Orbit projOrbit) {
			Orbit curOrbit = findPreviousOrbit(first);
			return difference(curOrbit.getOrbitalVelocityAtUT(UT), projOrbit.getOrbitalVelocityAtUT(UT));
		}

		// calculation function for mergeNodeDown
		private static Orbit findPreviousOrbit(ManeuverNode n) {
			PatchedConicSolver p = getSolver();
			int idx = p.maneuverNodes.IndexOf(n);
			if(idx > 0) {
				return p.maneuverNodes[idx-1].patch;
			} else {
				return FlightGlobals.ActiveVessel.orbit;
			}
		}

		// calculation function for mergeNodeDown
		private static Vector3d difference(Vector3d initial, Vector3d final) {
			return new Vector3d(-(initial.x - final.x), -(initial.y - final.y), -(initial.z - final.z)).xzy;
		}

		/// <summary>
		/// Formats the given double into meters.
		/// </summary>
		/// <returns>The string format, in meters.</returns>
		/// <param name="d">The double to format</param>
		public static string formatMeters(double d) {
			if(Math.Abs(d / 1000000.0) > 1) {
				// format as kilometers.
				return (d/1000.0).ToString("0.##") + "km";
			} else {
				// use meters
				if(Math.Abs(d) > 100000.0) {
					return d.ToString("F0") + "m";
				} else {
					return d.ToString("0.##") + "m";
				}
			}
		}

		/// <summary>
		/// Returns the orbit of the currently targeted item or null if there is none.
		/// </summary>
		/// <returns>The orbit or null.</returns>
		public static Orbit getTargetOrbit() {
			ITargetable tgt = FlightGlobals.fetch.VesselTarget;
			if(tgt != null) {
				// if we have a null vessel it's a celestial body
				if(tgt.GetVessel() == null) { return tgt.GetOrbit(); }
				// otherwise make sure we're not targeting ourselves.
				if(!FlightGlobals.fetch.activeVessel.Equals(tgt.GetVessel())) {
					return tgt.GetOrbit();
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the UT for the equatorial AN.
		/// </summary>
		/// <returns>The equatorial AN UT.</returns>
		/// <param name="o">The Orbit to calculate the UT from.</param>
		public static double getEquatorialANUT(Orbit o) {
			return o.GetUTforTrueAnomaly(o.GetTrueAnomalyOfZupVector(o.GetANVector()), 2);
		}

		/// <summary>
		/// Gets the UT for the ascending node in reference to the target orbit.
		/// </summary>
		/// <returns>The UT for the ascending node in reference to the target orbit.</returns>
		/// <param name="a">The orbit to find the UT on.</param>
		/// <param name="b">The target orbit.</param>
		public static double getTargetANUT(Orbit a, Orbit b) {
			Vector3d ANVector = Vector3d.Cross(b.h, a.GetOrbitNormal()).normalized;
			return a.GetUTforTrueAnomaly(a.GetTrueAnomalyOfZupVector(ANVector), 2);
		}

		/// <summary>
		/// Gets the UT for the equatorial DN.
		/// </summary>
		/// <returns>The equatorial DN UT.</returns>
		/// <param name="o">The Orbit to calculate the UT from.</param>
		public static double getEquatorialDNUT(Orbit o) {
			Vector3d DNVector = QuaternionD.AngleAxis(NodeTools.Angle360(o.LAN + 180), Planetarium.Zup.Z) * Planetarium.Zup.X;
			return o.GetUTforTrueAnomaly(o.GetTrueAnomalyOfZupVector(DNVector), 2);
		}

		/// <summary>
		/// Gets the UT for the descending node in reference to the target orbit.
		/// </summary>
		/// <returns>The UT for the descending node in reference to the target orbit.</returns>
		/// <param name="a">The orbit to find the UT on.</param>
		/// <param name="b">The target orbit.</param>
		public static double getTargetDNUT(Orbit a, Orbit b) {
			Vector3d DNVector = Vector3d.Cross(a.GetOrbitNormal(), b.h).normalized;
			return a.GetUTforTrueAnomaly(a.GetTrueAnomalyOfZupVector(DNVector), 2);
		}

		/// <summary>
		/// Adjusts the specified angle to between 0 and 360 degrees.
		/// </summary>
		/// <param name="d">The specified angle to restrict.</param>
        public static double Angle360(double d) {
            d %= 360;
            if(d < 0) {
				return d + 360;
			}
			return d;
        }

		/// <summary>
		/// Gets the ejection angle of the current maneuver node.
		/// </summary>
		/// <returns>The ejection angle in degrees.  Positive results are the angle from prograde, negative results are the angle from retrograde.</returns>
		/// <param name="nodeUT">Kerbal Spece Program Universal Time.</param>
		public static double getEjectionAngle(Orbit o, double nodeUT) {
			CelestialBody body = o.referenceBody;

			// Calculate the angle between the node's position and the reference body's velocity at nodeUT
			Vector3d prograde = body.orbit.getOrbitalVelocityAtUT(nodeUT);
			Vector3d position = o.getRelativePositionAtUT(nodeUT);
			double eangle = NodeTools.Angle360((Math.Atan2(prograde.y, prograde.x) - Math.Atan2(position.y, position.x)) * 180.0 / Math.PI);

			// Correct to angle from retrograde if needed.
			if(eangle > 180) {
				eangle = 180 - eangle;
			}

			return eangle;
		}

		/// <summary>
		/// Convenience function.
		/// </summary>
		/// <returns>The patched conic solver for the currently active vessel.</returns>
		public static PatchedConicSolver getSolver() {
			return FlightGlobals.ActiveVessel.patchedConicSolver;
		}

		public static Orbit findNextEncounter(ManeuverNode node) {
			System.Collections.ObjectModel.ReadOnlyCollection<Orbit> plan = node.solver.flightPlan.AsReadOnly();
			Orbit curOrbit = node.patch; // FlightGlobals.ActiveVessel.orbit;
			for(int k = plan.IndexOf(node.patch); k < plan.Count; k++) {
				Orbit o = plan[k];
				if(curOrbit.referenceBody.name != o.referenceBody.name && o.referenceBody.name != "Sun") {
					return o;
				}
			}
			return null;
		}

		/// <summary>
		/// Function to figure out which KeyCode was pressed.
		/// </summary>
		public static KeyCode fetchKey() {
			int enums = System.Enum.GetNames(typeof(KeyCode)).Length;
			for(int k = 0; k < enums; k++) {
				if(Input.GetKey((KeyCode)k)) {
					return (KeyCode)k;
				}
			}

			return KeyCode.None;
		}
	}

	public class PNOptions {
		public Rect mainWindowPos = new Rect(Screen.width / 10, 20, 250, 130);
		public Rect optionsWindowPos = new Rect(Screen.width / 3, 20, 250, 130);
		public Rect keymapperWindowPos = new Rect(Screen.width / 5, 20, 250, 130);
		public Rect clockWindowPos = new Rect(Screen.width / 3, Screen.height / 2, 195, 65);
		public Rect conicsWindowPos = new Rect(Screen.width / 5, Screen.height / 2, 250, 65);
		public Rect tripWindowPos = new Rect(Screen.width / 5, Screen.height / 5, 320, 65);

		public bool showManeuverPager = true;
		public bool showConicsAlways = false;
		public bool showClock = false;
		public bool showTrip = false;
		public bool showUTControls = false;
		public bool showEAngle = true;
		public bool showOrbitInfo = false;

		public KeyCode progInc = KeyCode.Keypad8;
		public KeyCode progDec = KeyCode.Keypad5;
		public KeyCode normInc = KeyCode.Keypad9;
		public KeyCode normDec = KeyCode.Keypad7;
		public KeyCode radiInc = KeyCode.Keypad6;
		public KeyCode radiDec = KeyCode.Keypad4;
		public KeyCode timeInc = KeyCode.Keypad3;
		public KeyCode timeDec = KeyCode.Keypad1;
		public KeyCode pageIncrement = KeyCode.Keypad0;
		public KeyCode pageConics = KeyCode.KeypadEnter;
		public KeyCode hideWindow = KeyCode.P;
		public KeyCode addWidget = KeyCode.O;
		public double increment = 1.0;
		public int conicsMode = 3;

		public void downIncrement() {
			if(increment == 0.01) {
				increment = 0.1;
			} else if(increment == 0.1) {
				increment = 1;
			} else if(increment == 1) {
				increment = 10;
			} else if(increment == 10) {
				increment = 100;
			} else if(increment == 100) {
				increment = 0.01;
			} else {
				increment = 1;
			}
		}

		public void upIncrement() {
			if(increment == 0.01) {
				increment = 100;
			} else if(increment == 0.1) {
				increment = 0.01;
			} else if(increment == 1) {
				increment = 0.1;
			} else if(increment == 10) {
				increment = 1;
			} else if(increment == 100) {
				increment = 10;
			} else {
				increment = 1;
			}
		}

		public void setConicsMode(int mode) {
			conicsMode = mode;
			NodeTools.changeConicsMode(conicsMode);
		}

		public void pageConicsMode() {
			conicsMode++;
			if(conicsMode < 0 || conicsMode > 4) {
				conicsMode = 0;
			}
			NodeTools.changeConicsMode(conicsMode);
		}
	}

	public class NodeManager {
		public NodeState curNodeState;
		public NodeState curState;
		public ManeuverNode node = null;
		public ManeuverNode nextNode = null;
		public bool changed = false;
		public bool encounter = false;
		public bool resizeMainWindow = false;
		public bool resizeClockWindow = false;

		public bool progradeParsed = true;
		public bool radialParsed = true;
		public bool normalParsed = true;
		public bool timeParsed = true;
		public string progradeText = "";
		public string radialText = "";
		public string normalText = "";
		public string timeText = "";

		public NodeManager() {
			curState = new NodeState();
		}

		public NodeManager(ManeuverNode n) {
			curState = new NodeState(n);
			curNodeState = new NodeState();
			node = n;
			updateCurrentNodeState();

			if(NodeTools.findNextEncounter(n) != null) {
				encounter = true;
			}
		}

		public NodeManager nextState() {
			if(nextNode != null) {
				return new NodeManager(nextNode);
			}
			if(NodeTools.findNextEncounter(node) != null) {
				encounter = true;
			}
			return this;
		}

		public void addPrograde(double d) {
			curState.deltaV.z += d;
			progradeText = curState.deltaV.z.ToString();
			changed = true;
		}

		public void setPrograde(String s) {
			double d;
			progradeText = s;
			if(s.EndsWith(".")) {
				progradeParsed = false;
				return;
			}
			progradeParsed = double.TryParse(progradeText, out d);
			if(progradeParsed) {
				if(d != curState.deltaV.z) {
					progradeText = d.ToString();
					curState.deltaV.z = d;
					changed = true;
				}
			}
		}

		public void addNormal(double d) {
			curState.deltaV.y += d;
			normalText = curState.deltaV.y.ToString();
			changed = true;
		}

		public void setNormal(String s) {
			if(normalText.Equals(s, StringComparison.Ordinal)) { return; }
			double d;
			normalText = s;
			if(s.EndsWith(".")) {
				normalParsed = false;
				return;
			}
			normalParsed = double.TryParse(normalText, out d);
			if(normalParsed) {
				if(d != curState.deltaV.y) {
					normalText = d.ToString();
					curState.deltaV.y = d;
					changed = true;
				}
			}
		}

		public void addRadial(double d) {
			curState.deltaV.x += d;
			radialText = curState.deltaV.x.ToString();
			changed = true;
		}

		public void setRadial(String s) {
			if(radialText.Equals(s, StringComparison.Ordinal)) { return; }
			double d;
			radialText = s;
			if(s.EndsWith(".")) {
				radialParsed = false;
				return;
			}
			radialParsed = double.TryParse(radialText, out d);
			if(radialParsed) {
				if(d != curState.deltaV.x) {
					radialText = d.ToString();
					curState.deltaV.x = d;
					changed = true;
				}
			}
		}

		public double currentUT() {
			return curState.UT;
		}

		public void addUT(double d) {
			curState.UT += d;
			timeText = curState.UT.ToString();
			changed = true;
		}

		public void setUT(double d) {
			curState.UT = d;
			timeText = curState.UT.ToString();
			changed = true;
		}

		public void setUT(String s) {
			if(timeText.Equals(s, StringComparison.Ordinal)) { return; }
			double d;
			timeText = s;
			if(s.EndsWith(".")) {
				timeParsed = false;
				return;
			}
			timeParsed = double.TryParse(timeText, out d);
			if(timeParsed) {
				if(d != curState.UT) {
					timeText = d.ToString();
					curState.UT = d;
					changed = true;
				}
			}
		}

		public double currentMagnitude() {
			return curState.deltaV.magnitude;
		}

		public bool hasNode() {
			if(node == null) { return false; }
			return true;
		}

		public void updateNode() {
			// Node manager policy:
			// if the manager has been changed from the last update manager snapshot, take the manager
			// UNLESS
			// if the node has been changed from the last update node snapshot, take the node
			if(curNodeState.compare(node)) {
				// the node hasn't changed, do our own thing
				if(changed) {
					if(node.attachedGizmo != null) {
						node.attachedGizmo.DeltaV = curState.getVector();
						node.attachedGizmo.UT = curState.UT;
					}
					node.OnGizmoUpdated(curState.getVector(), curState.UT);
					updateCurrentNodeState();
					changed = false; // new
				}
			} else {
				// the node has changed, take the node's new information for ourselves.
				updateCurrentNodeState();
				curState.update(node);
			}
		}

		private void updateCurrentNodeState() {
			curNodeState.update(node);
			progradeText = node.DeltaV.z.ToString();
			normalText = node.DeltaV.y.ToString();
			radialText = node.DeltaV.x.ToString();
			timeText = node.UT.ToString();
		}
	}

	public class NodeState {
		public Vector3d deltaV;
		public double UT;

		public NodeState() {
			deltaV = new Vector3d();
			UT = 0;
		}

		public NodeState(Vector3d dv, double u) {
			deltaV = new Vector3d(dv.x, dv.y, dv.z);
			UT = u;
		}

		public NodeState(ManeuverNode m) {
			deltaV = new Vector3d(m.DeltaV.x, m.DeltaV.y, m.DeltaV.z);
			UT = m.UT;
		}

		public void update(ManeuverNode m) {
			deltaV.x = m.DeltaV.x;
			deltaV.y = m.DeltaV.y;
			deltaV.z = m.DeltaV.z;
			UT = m.UT;
		}

		public Vector3d getVector() {
			return new Vector3d(deltaV.x, deltaV.y, deltaV.z);
		}

		public bool compare(ManeuverNode m) {
			if(deltaV.x != m.DeltaV.x || deltaV.y != m.DeltaV.y || deltaV.z != m.DeltaV.z || UT != m.UT) {
				return false;
			}
			return true;
		}

		public void createManeuverNode(PatchedConicSolver p) {
			ManeuverNode newnode = p.AddManeuverNode(UT);
			newnode.OnGizmoUpdated(deltaV, UT);
		}
	}
}
