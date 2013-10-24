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
		public void changeConicsMode(int mode) {
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
		public void CreateNodeGizmo(ManeuverNode node) {
			if(node.attachedGizmo != null) { return; }
			PatchRendering pr = FlightGlobals.ActiveVessel.patchedConicRenderer.FindRenderingForPatch(node.patch);
			node.AttachGizmo(MapView.ManeuverNodePrefab, FlightGlobals.ActiveVessel.patchedConicRenderer, pr);
		}

		/// <summary>
		/// Converts the UT to human-readable Kerbal local time.
		/// </summary>
		/// <returns>The converted time.</returns>
		/// <param name="UT">Kerbal Spece Program Universal Time.</param>
		public String convertUTtoHumanTime(double UT) {
			long secs = (long)(UT % 60);
			long mins = (long)((UT / 60) % 60);
			long hour = (long)((UT / 3600) % 24);
			long day = (long)((UT / 86400) % 365) + 1;  // Ensure we don't get a "Day 0" here.
			long year = (long)(UT / (86400 * 365)) + 1; // Ensure we don't get a "Year 0" here.

			return "Year " + year + " Day " + day + " " + hour + ":" + (mins < 10 ? "0" : "") + mins + ":" + (secs < 10 ? "0" : "") + secs;
		}

		/// <summary>
		/// Converts Radians to Degrees
		/// </summary>
		/// <returns>The converted radians</returns>
		/// <param name="d">The radians to convert</param>
		public double radToDeg(double d) {
			return d * 57.295779513082323;
		}

		/// <summary>
		/// Formats the given double into meters.
		/// </summary>
		/// <returns>The string format, in meters.</returns>
		/// <param name="d">The double to format</param>
		public string formatMeters(double d) {
			if(Math.abs(d / 1000000.0) > 1) {
				// format as kilometers.
				return (d/1000.0).ToString("F2") + "km";
			} else {
				// use meters
				if(Math.abs(d) > 100000.0) {
					return d.ToString("D") + "m";
				} else {
					return d.ToString("F2") + "m";
				}
			}
		}

		/// <summary>
		/// Gets the ejection angle of the current maneuver node.
		/// </summary>
		/// <returns>The ejection angle in degrees.  Positive results are the angle from prograde, negative results are the angle from retrograde.</returns>
		/// <param name="nodeUT">Kerbal Spece Program Universal Time.</param>
		public double getEjectionAngle(Orbit o, double nodeUT) {
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
		public PatchedConicSolver getSolver() {
			return FlightGlobals.ActiveVessel.patchedConicSolver;
		}

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
		
		/// <summary>
		/// Function to figure out which KeyCode was pressed.
		/// </summary>
		public KeyCode fetchKey() {
			int enums = System.Enum.GetNames(typeof(KeyCode)).Length;
			for(int k = 0; k < enums; k++) {
				if(Input.GetKey((KeyCode)k)) {
					return (KeyCode)k;
				}
			}

			return KeyCode.None;
		}
	}
}
