using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

/******************************************************************************
 * Copyright (c) 2013, Justin Bengtson
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
		/// Converts Radians to Degrees
		/// </summary>
		/// <returns>The converted radians</returns>
		/// <param name="d">The radians to convert</param>
		public static double radToDeg(double d) {
			return d * 57.295779513082323;
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
		/// Gets the ejection angle of the current maneuver node.
		/// </summary>
		/// <returns>The ejection angle in degrees.  Positive results are the angle from prograde, negative results are the angle from retrograde.</returns>
		/// <param name="nodeUT">Kerbal Spece Program Universal Time.</param>
		public static double getEjectionAngle(Orbit o, double nodeUT) {
			CelestialBody body = o.referenceBody;

			// Convert the node's orbit position to world space and get the raw ejection angle
			// Vector3d worldpos = body.position + thisOrbit.getRelativePositionAtUT(nodeUT).xzy;
			Vector3d worldpos = body.position + o.getRelativePositionAtUT(nodeUT).xzy;
			double eangle = ((body.GetLongitude(worldpos) + body.rotationAngle)	- (body.orbit.LAN / 360 + body.orbit.orbitPercent) * 360) % 360;

			// Correct negative angles.
			if(eangle < 0) {
				eangle += 360;
			}

			// Correct to angle from retrograde if needed.
			if(eangle < 270) {
				eangle = 90 - eangle;
				if(eangle < 0) {
					eangle = (180 - Math.Abs(eangle)) * -1;
				}
			} else {
				eangle = 450 - eangle;
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

		/*
		public Orbit findNextEncounter(ManeuverNode node) {
			List<Orbit> plan = node.solver.flightPlan;
			Orbit curOrbit = FlightGlobals.ActiveVessel.orbit;
			foreach(Orbit o in plan) {
				if(curOrbit.referenceBody.name != o.referenceBody.name && o.referenceBody.name != "Sun") {
					return o;
				}
			}
			return null;
		}
		*/
		
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
	}

	public class PreciseNodeState {
		public ManeuverNode node = null;
		public ManeuverNode nextNode = null;
		public Vector3d deltaV;
		public double lastUT = 0;
		public bool changed = false;
		public bool encounter = false;
		public bool resizeMainWindow = false;
		public bool resizeClockWindow = false;

		public PreciseNodeState() {
			deltaV = new Vector3d();
		}

		public PreciseNodeState(ManeuverNode n) {
			deltaV = new Vector3d(n.DeltaV.x, n.DeltaV.y, n.DeltaV.z);
			lastUT = n.UT;
			node = n;
			if(NodeTools.findNextEncounter(n) != null) {
				encounter = true;
			}
		}

		public PreciseNodeState nextState() {
			if(nextNode != null) {
				return new PreciseNodeState(nextNode);
			}
			return new PreciseNodeState(node);
		}

		public void addPrograde(double d) {
			deltaV.z += d;
			changed = true;
		}

		public void setPrograde(String s) {
			double d;
			if(double.TryParse(s, out d)) {
				if(d != deltaV.z) {
					deltaV.z = d;
					changed = true;
				}
			}
		}

		public void addNormal(double d) {
			deltaV.y += d;
			changed = true;
		}

		public void setNormal(String s) {
			double d;
			if(double.TryParse(s, out d)) {
				if(d != deltaV.y) {
					deltaV.y = d;
					changed = true;
				}
			}
		}

		public void addRadial(double d) {
			deltaV.x += d;
			changed = true;
		}

		public void setRadial(String s) {
			double d;
			if(double.TryParse(s, out d)) {
				if(d != deltaV.x) {
					deltaV.x = d;
					changed = true;
				}
			}
		}

		public void addUT(double d) {
			lastUT += d;
			changed = true;
		}

		public void setUT(double d) {
			lastUT = d;
			changed = true;
		}

		public void setUT(String s) {
			double d;
			if(double.TryParse(s, out d)) {
				if(d != lastUT) {
					lastUT = d;
					changed = true;
				}
			}
		}

		public bool hasNode() {
			return (node != null);
		}

		public void updateNode() {
			if(changed) {
				node.OnGizmoUpdated(deltaV, lastUT);
			}
		}

		private void dumpPatchedConicRenderer() {
			PatchedConicRenderer p = FlightGlobals.ActiveVessel.patchedConicRenderer;
			for(int i = 0; i < p.flightPlanRenders.Count; i++) {
				Debug.Log("FPRender-" + i + ": " + p.flightPlanRenders[i].ToString());
			}
			for(int i = 0; i < p.patchRenders.Count; i++) {
				Debug.Log("PRender-" + i + ": " + p.patchRenders[i].ToString());
			}
			Debug.Log("Solver: " + p.solver.ToString());

		}
	}
}
