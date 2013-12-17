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
	[KSPAddon(KSPAddon.Startup.Flight, false)]

	public class PreciseNode : MonoBehaviour {
		public PluginConfiguration config;
		private PNOptions options = new PNOptions();
		private PreciseNodeState curState = new PreciseNodeState();

		private bool configLoaded = false;
		private bool conicsLoaded = false;
		private bool shown = true;
		private bool waitForKey = false;
		private bool showOptions = false;
		private bool showKeymapper = false;
		private byte currentWaitKey = 255;
		private int conicsMode = 3;
		private double keyWaitTime = 0.0;
		private double increment = 1.0;

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		public void Awake() {
			CancelInvoke();
			loadConfig();
		}

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		public void OnDisable() {
			saveConfig();
		}

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		public void Update() {
			if(canShowNodeEditor) {
				processKeyInput();
			}
		}

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		public void OnGUI() {
			/*
			if(Event.current.type == EventType.Layout) {
				// On layout we should see if we have nodes to act on.
				PatchedConicSolver solver = NodeTools.getSolver();
				if(solver.maneuverNodes.Count > 0) {
					if(curState.node == null || !solver.maneuverNodes.Contains(curState.node)) {
						// get the first one if we can't find the current or it's null
						curState = new PreciseNodeState(solver.maneuverNodes[0]);
						options.clockWindowPos.height = 65;
					} else if(curState.node != null) {
						curState.updateNode();
						if(curState.resizeMainWindow) {
							options.mainWindowPos.height = 250;
						}
						if(curState.resizeClockWindow) {
							options.clockWindowPos.height = 65;
						}
						curState = curState.nextState();
					}
				} else {
					if(curState.node != null) {
						curState = new PreciseNodeState();
						curState.resizeClockWindow = true;
					}
				}
			}
			*/
			if(Event.current.type == EventType.Layout) {
				// On layout we should see if we have nodes to act on.
				if(curState.resizeMainWindow) {
					options.mainWindowPos.height = 250;
				}
				if(curState.resizeClockWindow) {
					options.clockWindowPos.height = 65;
				}
			}
			if(canShowNodeEditor) {
				if(!conicsLoaded) {
					NodeTools.changeConicsMode(conicsMode);
					conicsLoaded = true;
				}
				if(shown) {
					drawGUI();
				} else if(canShowConicsWindow) {
					drawConicsGUI();
				}
			} else if(canShowConicsWindow) {
				drawConicsGUI();
			}
			if(canShowClock) {
				drawClockGUI();
			}
			if(Event.current.type == EventType.Repaint) {
				// On layout we should see if we have nodes to act on.
				PatchedConicSolver solver = NodeTools.getSolver();
				if(solver.maneuverNodes.Count > 0) {
					if(!curState.hasNode() || !solver.maneuverNodes.Contains(curState.node)) {
						// get the first one if we can't find the current or it's null
						curState = new PreciseNodeState(solver.maneuverNodes[0]);
					} else if(curState.hasNode()) {
						curState.updateNode();
						curState = curState.nextState();
					}
				} else {
					if(curState.hasNode()) {
						curState = new PreciseNodeState();
						curState.resizeClockWindow = true;
					}
				}
			}
		}

		/// <summary>
		/// Draw Node Editor and Options GUI
		/// </summary>
		public void drawGUI() {
			GUI.skin = null;
			options.mainWindowPos = GUILayout.Window(21349, options.mainWindowPos, drawMainWindow, "Precise Node", GUILayout.ExpandHeight(true));
			if(showOptions) {
				options.optionsWindowPos = GUILayout.Window(21350, options.optionsWindowPos, drawOptionsWindow, "Precise Node Options", GUILayout.ExpandHeight(true));
			}
			if(showKeymapper) {
				options.keymapperWindowPos = GUILayout.Window(21351, options.keymapperWindowPos, drawKeymapperWindow, "Precise Node Keys", GUILayout.ExpandHeight(true));
			}
			if(options.showTrip) {
				options.tripWindowPos = GUILayout.Window(21352, options.tripWindowPos, drawTripWindow, "Trip Info", GUILayout.ExpandHeight(true));
			}
		}

		/// <summary>
		/// Draw Clock GUI
		/// </summary>
		public void drawClockGUI() {
			GUI.skin = null;
			options.clockWindowPos = GUILayout.Window(21353, options.clockWindowPos, drawClockWindow, "Clock", GUILayout.ExpandHeight(true));
		}

		/// <summary>
		/// Draw Conics GUI
		/// </summary>
		public void drawConicsGUI() {
			GUI.skin = null;
			options.conicsWindowPos = GUILayout.Window(21354, options.conicsWindowPos, drawConicsWindow, "Conics Controls", GUILayout.ExpandHeight(true));
		}

		/// <summary>
		/// Draws the Node Editor window.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void drawMainWindow(int id) {
			Color defaultColor = GUI.backgroundColor;
			PatchedConicSolver solver = NodeTools.getSolver();

			String timeHuman = NodeTools.convertUTtoHumanTime(curState.node.UT);
			String timeUT = curState.node.UT.ToString("0.##");
			String prograde = curState.node.DeltaV.z.ToString("0.##") + "m/s";
			String normal = curState.node.DeltaV.y.ToString("0.##") + "m/s";
			String radial = curState.node.DeltaV.x.ToString("0.##") + "m/s";
			String eangle = "n/a";

			if(FlightGlobals.ActiveVessel.orbit.referenceBody.name != "Sun") {
				eangle = NodeTools.getEjectionAngle(FlightGlobals.ActiveVessel.orbit, curState.node.UT).ToString("0.##") + "°";
			}

			// Options button
			if(GUI.Button(new Rect(options.mainWindowPos.width - 48, 2, 22, 18), "O")) {
				showOptions = true;
			}
			// Keymapping button
			if(GUI.Button(new Rect(options.mainWindowPos.width - 24, 2, 22, 18), "K")) {
				showKeymapper = true;
			}

			GUILayout.BeginVertical();

			if(options.showManeuverPager) {
				// Maneuver node paging controls
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("<")) {
					int count = solver.maneuverNodes.Count;
					if(count > 1) {
						// get the previous or last node
						int idx = solver.maneuverNodes.IndexOf(curState.node);
						if(idx == 0) {
							curState.nextNode = solver.maneuverNodes[--count];
						} else {
							curState.nextNode = solver.maneuverNodes[--idx];
						}
					}
				}
				if(GUILayout.Button("Editing Node " + (solver.maneuverNodes.IndexOf(curState.node) + 1))) {
					MapView.MapCamera.SetTarget(curState.node.scaledSpaceTarget);
				}
				// GUILayout.Label("Editing Node " + (solver.maneuverNodes.IndexOf(node) + 1), GUILayout.Width(100));
				if(GUILayout.Button(">")) {
					int count = solver.maneuverNodes.Count;
					if(count > 1) {
						// get the previous or last node
						int idx = solver.maneuverNodes.IndexOf(curState.node);
						if(idx == count - 1) {
							curState.nextNode = solver.maneuverNodes[0];
						} else {
							curState.nextNode = solver.maneuverNodes[++idx];
						}
					}
				}
				GUILayout.EndHorizontal();
			}

			// Human-readable time
			GUILayout.BeginHorizontal();
			GUILayout.Label("Time:", GUILayout.Width(100));
			GUILayout.Label(timeHuman, GUILayout.Width(130));
			GUILayout.EndHorizontal();

			// Increment buttons
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment:", GUILayout.Width(100));
			if(increment == 0.01) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("0.01")) {
				increment = 0.01;
			}
			GUI.backgroundColor = defaultColor;
			if(increment == 0.1) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("0.1")) {
				increment = 0.1;
			}
			GUI.backgroundColor = defaultColor;
			if(increment == 1) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("1")) {
				increment = 1;
			}
			GUI.backgroundColor = defaultColor;
			if(increment == 10) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("10")) {
				increment = 10;
			}
			GUI.backgroundColor = defaultColor;
			if(increment == 100) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("100")) {
				increment = 100;
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			// Universal time controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("UT:", GUILayout.Width(100));
			GUI.backgroundColor = Color.green;
			String check = GUILayout.TextField(curState.lastUT.ToString(), GUILayout.Width(100));
			curState.setUT(check);

			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				curState.addUT(increment * -1.0);
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				curState.addUT(increment);
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			if(options.showUTControls) {
				GUILayout.BeginHorizontal();
				GUI.backgroundColor = Color.yellow;
				if(GUILayout.Button("Peri")) {
					curState.setUT(Planetarium.GetUniversalTime() + curState.node.patch.timeToPe);
				}
				GUI.backgroundColor = Color.magenta;
				if(GUILayout.Button("-10K")) {
					curState.addUT(-10000);
				}
				GUI.backgroundColor = Color.red;
				if(GUILayout.Button("-1K")) {
					curState.addUT(-1000);
				}
				GUI.backgroundColor = Color.green;
				if(GUILayout.Button("+1K")) {
					curState.addUT(1000);
				}
				GUI.backgroundColor = Color.cyan;
				if(GUILayout.Button("+10K")) {
					curState.addUT(10000);
				}
				GUI.backgroundColor = Color.blue;
				if(GUILayout.Button("Apo")) {
					curState.setUT(Planetarium.GetUniversalTime() + curState.node.patch.timeToAp);
				}
				GUI.backgroundColor = defaultColor;
				GUILayout.EndHorizontal();
			}

			// Prograde controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Prograde:", GUILayout.Width(100));
			check = GUILayout.TextField(curState.deltaV.z.ToString(), GUILayout.Width(100));
			curState.setPrograde(check);
			// GUILayout.Label(prograde, GUILayout.Width(100));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				curState.addPrograde(increment * -1.0);
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				curState.addPrograde(increment);
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			// Normal controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Normal:", GUILayout.Width(100));
			check = GUILayout.TextField(curState.deltaV.y.ToString(), GUILayout.Width(100));
			curState.setNormal(check);
			// GUILayout.Label(normal, GUILayout.Width(100));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				curState.addNormal(increment * -1.0);
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				curState.addNormal(increment);
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			// radial controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Radial:", GUILayout.Width(100));
			check = GUILayout.TextField(curState.deltaV.x.ToString(), GUILayout.Width(100));
			curState.setRadial(check);
			// GUILayout.Label(radial, GUILayout.Width(100));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				curState.addRadial(increment * -1.0);
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				curState.addRadial(increment);
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			// total delta-V display
			GUILayout.BeginHorizontal();
			GUILayout.Label("Total delta-V:",GUILayout.Width(100));
			GUILayout.Label(curState.node.DeltaV.magnitude.ToString("0.##") + "m/s");
			GUILayout.EndHorizontal();

			// Ejection angle
			if(options.showEAngle) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Ejection Angle:", GUILayout.Width(100));
				GUILayout.Label(eangle, GUILayout.Width(100));
				GUILayout.EndHorizontal();
			}

			// Additional Information
			if(options.showOrbitInfo) {
				// Find the next encounter, if any, in our flight plan.
				if(curState.encounter) {
					Orbit nextEnc = NodeTools.findNextEncounter(curState.node);
					// Next encounter periapsis
					GUILayout.BeginHorizontal();
					GUILayout.Label("(" + nextEnc.referenceBody.name + ") Pe:", GUILayout.Width(100));
					GUILayout.Label(NodeTools.formatMeters(nextEnc.PeA), GUILayout.Width(100));
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();

					GUI.backgroundColor = defaultColor;
					GUILayout.Label("", GUILayout.Width(100));
					if(GUILayout.Button("Focus on " + nextEnc.referenceBody.name)) {
						MapView.MapCamera.SetTarget(nextEnc.referenceBody.name);
					}
					GUILayout.EndHorizontal();
				} else {
					if(curState.node.solver.flightPlan.Count > 1) {
						// output the apoapsis and periapsis of our projected orbit.
						GUILayout.BeginHorizontal();
						GUILayout.Label("Apoapsis:", GUILayout.Width(100));
						// GUILayout.Label(tools.formatMeters(plan[1].ApA), GUILayout.Width(100));
						GUILayout.Label(NodeTools.formatMeters(curState.node.nextPatch.ApA), GUILayout.Width(100));
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						GUILayout.Label("Periapsis:", GUILayout.Width(100));
						// GUILayout.Label(tools.formatMeters(plan[1].PeA), GUILayout.Width(100));
						GUILayout.Label(NodeTools.formatMeters(curState.node.nextPatch.PeA), GUILayout.Width(100));
						GUILayout.EndHorizontal();
					}
				}
			}

			// Conics mode controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Conics mode: ", GUILayout.Width(100));
			if(conicsMode == 0) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("0")) {
				conicsMode = 0;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 1) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("1")) {
				conicsMode = 1;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 2) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("2")) {
				conicsMode = 2;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 3) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("3")) {
				conicsMode = 3;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 4) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("4")) {
				conicsMode = 4;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			// conics patch limit editor.
			GUILayout.BeginHorizontal();
			GUILayout.Label("Change Conics Samples", GUILayout.Width(200));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				solver.DecreasePatchLimit();
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				solver.IncreasePatchLimit();
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			// trip info button
			GUILayout.BeginHorizontal();
			if(options.showTrip) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("Trip Info")) {
				options.showTrip = !options.showTrip;
			}
			GUI.backgroundColor = defaultColor;
			if(GUILayout.Button("Focus on Vessel")) {
				MapView.MapCamera.SetTarget(FlightGlobals.ActiveVessel.vesselName);
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws the Clock window.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void drawClockWindow(int id) {
			Color defaultColor = GUI.backgroundColor;
			double timeNow = Planetarium.GetUniversalTime();
			String timeUT = timeNow.ToString("F0");
			String timeHuman = NodeTools.convertUTtoHumanTime(timeNow);

			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Time:", GUILayout.Width(35));
			GUILayout.Label(timeHuman, GUILayout.Width(150));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("UT:", GUILayout.Width(35));
			GUILayout.Label(Math.Floor(timeNow).ToString("F0"), GUILayout.Width(150));
			GUILayout.EndHorizontal();

			if(curState.hasNode() && NodeTools.getSolver().maneuverNodes.Count > 0) {
				double next = timeNow - NodeTools.getSolver().maneuverNodes[0].UT;
				GUILayout.BeginHorizontal();
				GUILayout.Label("Next:", GUILayout.Width(35));
				string labelText = "";
				if(next < 0) {
					labelText = "T- " + NodeTools.convertUTtoHumanDuration(next);
				} else {
					labelText = "T+ " + NodeTools.convertUTtoHumanDuration(next);
				}
				GUILayout.Label(labelText, GUILayout.Width(150));
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws the Conics window.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void drawConicsWindow(int id) {
			PatchedConicSolver solver = NodeTools.getSolver();
			Color defaultColor = GUI.backgroundColor;

			GUILayout.BeginVertical();

			// Conics mode controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Conics mode: ", GUILayout.Width(100));
			if(conicsMode == 0) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("0")) {
				conicsMode = 0;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 1) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("1")) {
				conicsMode = 1;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 2) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("2")) {
				conicsMode = 2;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 3) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("3")) {
				conicsMode = 3;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 4) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("4")) {
				conicsMode = 4;
				NodeTools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			// conics patch limit editor.
			GUILayout.BeginHorizontal();
			GUILayout.Label("Change Conics Samples", GUILayout.Width(200));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				solver.DecreasePatchLimit();
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				solver.IncreasePatchLimit();
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws the Options window.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void drawOptionsWindow(int id) {
			Color defaultColor = GUI.backgroundColor;

			// Close button
			if(GUI.Button(new Rect(options.optionsWindowPos.width - 24, 2, 22, 18), "X")) {
				showOptions = false;
			}

			GUILayout.BeginVertical();
			options.showConicsAlways = GUILayout.Toggle(options.showConicsAlways, "Always Show Conics Controls");
			options.showClock = GUILayout.Toggle(options.showClock, "Show Clock");
			// use a temp variable so we can check whether the main window needs resizing.
			bool temp = GUILayout.Toggle(options.showManeuverPager, "Show Maneuver Pager");
			if(temp != options.showManeuverPager) {
				options.showManeuverPager = temp;
				curState.resizeMainWindow = true;
			}
			temp = GUILayout.Toggle(options.showUTControls, "Show Additional UT Controls");
			if(temp != options.showUTControls) {
				options.showUTControls = temp;
				curState.resizeMainWindow = true;
			}
			temp = GUILayout.Toggle(options.showEAngle, "Show Ejection Angle");
			if(temp != options.showEAngle) {
				options.showEAngle = temp;
				curState.resizeMainWindow = true;
			}
			temp = GUILayout.Toggle(options.showOrbitInfo, "Show Orbit Information");
			if(temp != options.showOrbitInfo) {
				options.showOrbitInfo = temp;
				curState.resizeMainWindow = true;
			}
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws the Keymapper window.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void drawKeymapperWindow(int id) {
			Color defaultColor = GUI.backgroundColor;

			// Close button
			if(GUI.Button(new Rect(options.keymapperWindowPos.width - 24, 2, 22, 18), "X")) {
				showKeymapper = false;
			}

			GUILayout.BeginVertical();

			// Set window control
			GUILayout.BeginHorizontal();
			GUILayout.Label("Hide/Show Window: " + options.hideWindow.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Hide/Show Window...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.HIDEWINDOW;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			// Set add node widget button
			GUILayout.BeginHorizontal();
			GUILayout.Label("Open Node Gizmo: " + options.addWidget.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Open Node Gizmo...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.ADDWIDGET;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			// Set prograde controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Prograde: " + options.progInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Increment Prograde...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.PROGINC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Prograde: " + options.progDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Decrement Prograde...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.PROGDEC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			// set normal controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Normal: " + options.normInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Increment Normal...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.NORMINC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Normal: " + options.normDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Decrement Normal...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.NORMDEC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			// set radial controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Radial: " + options.radiInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Increment Radial...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.RADIINC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Radial: " + options.radiDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Decrement Radial...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.RADIDEC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			// set time controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Time: " + options.timeInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Increment Time...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.TIMEINC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Time: " + options.timeDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Decrement Time...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.TIMEDEC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			// set paging controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Page Increment: " + options.pageIncrement.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Page Increment...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.PAGEINC;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Page Conics: " + options.pageConics.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Page Conics Mode...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.PAGECON;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		public void drawTripWindow(int id) {
			PatchedConicSolver solver = NodeTools.getSolver();

			GUILayout.BeginVertical();
			if(solver.maneuverNodes.Count < 1) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("No nodes to show.", GUILayout.Width(200));
				GUILayout.EndHorizontal();
			} else {
				double total = 0.0;
				double timeNow = Planetarium.GetUniversalTime();

				GUILayout.BeginHorizontal();
				GUILayout.Label("", GUILayout.Width(60));
				GUILayout.Label("Δv", GUILayout.Width(90));
				GUILayout.Label("Time Until", GUILayout.Width(150));
				GUILayout.EndHorizontal();

				foreach(ManeuverNode curNode in solver.maneuverNodes) {
					int idx = solver.maneuverNodes.IndexOf(curNode);
					double timeDiff = curNode.UT - timeNow;
					GUILayout.BeginHorizontal();
					GUILayout.Label("Node " + idx, GUILayout.Width(60));
					GUILayout.Label(curNode.DeltaV.magnitude.ToString("F2") + "m/s", GUILayout.Width(90));
					GUILayout.Label(NodeTools.convertUTtoHumanDuration(timeDiff), GUILayout.Width(150));
					GUILayout.EndHorizontal();
					total += curNode.DeltaV.magnitude;
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("", GUILayout.Width(60));
				GUILayout.Label(total.ToString("F2") + "m/s", GUILayout.Width(90));
				GUILayout.Label("", GUILayout.Width(150));
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Returns whether the Node Editor can be shown based on a number of global factors.
		/// </summary>
		/// <value><c>true</c> if the Node Editor can be shown; otherwise, <c>false</c>.</value>
		private bool canShowNodeEditor {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled && NodeTools.getSolver().maneuverNodes.Count > 0 && curState.hasNode();
			}
		}

		/// <summary>
		/// Returns whether the Conics Window can be shown based on a number of global factors.
		/// </summary>
		/// <value><c>true</c> if the Conics Window can be shown; otherwise, <c>false</c>.</value>
		private bool canShowConicsWindow {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled && options.showConicsAlways;
			}
		}

		/// <summary>
		/// Returns whether the Clock Window can be shown based on a number of global factors.
		/// </summary>
		/// <value><c>true</c> if the Clock Window can be shown; otherwise, <c>false</c>.</value>
		private bool canShowClock {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && RenderingManager.fetch.enabled && options.showClock;
			}
		}

		/// <summary>
		/// Processes keyboard input.
		/// </summary>
		private void processKeyInput() {
			if(!Input.anyKeyDown) {
				return;
			}

			// process any key input for settings
			if(waitForKey) {
				KeyCode key = NodeTools.fetchKey();
				// if the time is up or we have no key to process, reset.
				if(((keyWaitTime + 5.0) < Planetarium.GetUniversalTime()) || key == KeyCode.None) {
					currentWaitKey = 255;
					waitForKey = false;
					return;
				}

				// which key are we waiting for?
				switch(currentWaitKey) {
				case (byte)KEYS.PROGINC:
					options.progInc = key;
					break;
				case (byte)KEYS.PROGDEC:
					options.progDec = key;
					break;
				case (byte)KEYS.NORMINC:
					options.normInc = key;
					break;
				case (byte)KEYS.NORMDEC:
					options.normDec = key;
					break;
				case (byte)KEYS.RADIINC:
					options.radiInc = key;
					break;
				case (byte)KEYS.RADIDEC:
					options.radiDec = key;
					break;
				case (byte)KEYS.TIMEINC:
					options.timeInc = key;
					break;
				case (byte)KEYS.TIMEDEC:
					options.timeDec = key;
					break;
				case (byte)KEYS.PAGEINC:
					options.pageIncrement = key;
					break;
				case (byte)KEYS.PAGECON:
					options.pageConics = key;
					break;
				case (byte)KEYS.HIDEWINDOW:
					options.hideWindow = key;
					break;
				case (byte)KEYS.ADDWIDGET:
					options.addWidget = key;
					break;
				}
				currentWaitKey = 255;
				waitForKey = false;
				return;
			}

			// process normal keyboard input
			// change increment
			if(Input.GetKeyDown(options.pageIncrement)) {
				if(Event.current.alt) {
					// change increment
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
				} else {
					// change increment
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
			}
			// prograde increment
			if(Input.GetKeyDown(options.progInc)) {
				curState.addPrograde(increment);
			}
			// prograde decrement
			if(Input.GetKeyDown(options.progDec)) {
				curState.addPrograde(increment * -1.0);
			}
			// normal increment
			if(Input.GetKeyDown(options.normInc)) {
				curState.addNormal(increment);
			}
			// normal decrement
			if(Input.GetKeyDown(options.normDec)) {
				curState.addNormal(increment * -1.0);
			}
			// radial increment
			if(Input.GetKeyDown(options.radiInc)) {
				curState.addRadial(increment);
			}
			// radial decrement
			if(Input.GetKeyDown(options.radiDec)) {
				curState.addRadial(increment * -1.0);
			}
			// UT increment
			if(Input.GetKeyDown(options.timeInc)) {
				curState.addUT(increment);
			}
			// UT decrement
			if(Input.GetKeyDown(options.timeDec)) {
				curState.addUT(increment * -1.0);
			}
			// Page Conics
			if(Input.GetKeyDown(options.pageConics)) {
				if(conicsMode < 0) {
					conicsMode = 0;
				}
				conicsMode++;
				if(conicsMode > 4) {
					conicsMode = 0;
				}
				NodeTools.changeConicsMode(conicsMode);
			}
			// hide/show window
			if(Input.GetKeyDown(options.hideWindow)) {
				shown = !shown;
			}
			// open node gizmo
			if(Input.GetKeyDown(options.addWidget)) {
				NodeTools.CreateNodeGizmo(curState.node);
			}
		}

		/// <summary>
		/// Load any saved configuration from file.
		/// </summary>
		private void loadConfig() {
			Debug.Log("Loading PreciseNode settings.");
			if(!configLoaded) {
				config = KSP.IO.PluginConfiguration.CreateForType<PreciseNode>(null);
				config.load();
				configLoaded = true;

				try {
					conicsMode = config.GetValue<int>("conicsMode", 3);
					options.mainWindowPos.x = config.GetValue<int>("mainWindowX", Screen.width / 10);
					options.mainWindowPos.y = config.GetValue<int>("mainWindowY", 20);
					options.optionsWindowPos.x = config.GetValue<int>("optWindowX", Screen.width / 3);
					options.optionsWindowPos.y = config.GetValue<int>("optWindowY", 20);
					options.keymapperWindowPos.x = config.GetValue<int>("keyWindowX", Screen.width / 5);
					options.keymapperWindowPos.y = config.GetValue<int>("keyWindowY", 20);
					options.clockWindowPos.x = config.GetValue<int>("clockWindowX", Screen.width / 3);
					options.clockWindowPos.y = config.GetValue<int>("clockWindowY", Screen.height / 2);
					options.conicsWindowPos.x = config.GetValue<int>("conicsWindowX", Screen.width / 5);
					options.conicsWindowPos.y = config.GetValue<int>("conicsWindowY", Screen.height / 2);
					options.tripWindowPos.x = config.GetValue<int>("tripWindowX", Screen.width / 5);
					options.tripWindowPos.y = config.GetValue<int>("tripWindowY", Screen.height / 5);
					options.showClock = config.GetValue<bool>("showClock", false);
					options.showEAngle = config.GetValue<bool>("showEAngle", true);
					options.showConicsAlways = config.GetValue<bool>("showConicsAlways", false);
					options.showOrbitInfo = config.GetValue<bool>("showOrbitInfo", false);
					options.showUTControls = config.GetValue<bool>("showUTControls", false);
					options.showManeuverPager = config.GetValue<bool>("showManeuverPager", true);

					string temp = config.GetValue<String>("progInc", "Keypad8");
					options.progInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("progDec", "Keypad5");
					options.progDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("normInc", "Keypad9");
					options.normInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("normDec", "Keypad7");
					options.normDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("radiInc", "Keypad6");
					options.radiInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("radiDec", "Keypad4");
					options.radiDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("timeInc", "Keypad3");
					options.timeInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("timeDec", "Keypad1");
					options.timeDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("pageIncrement", "Keypad0");
					options.pageIncrement = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("pageConics", "KeypadEnter");
					options.pageConics = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("hideWindow", "P");
					options.hideWindow = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("addWidget", "O");
					options.addWidget = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
				} catch(ArgumentException) {
					// do nothing here, the defaults are already set
				}
			}
		}

		/// <summary>
		/// Save our configuration to file.
		/// </summary>
		private void saveConfig() {
			Debug.Log("Saving PreciseNode settings.");
			if(!configLoaded) {
				config = KSP.IO.PluginConfiguration.CreateForType<PreciseNode>(null);
			}

			config["conicsMode"] = conicsMode;
			config["progInc"] = options.progInc.ToString();
			config["progDec"] = options.progDec.ToString();
			config["normInc"] = options.normInc.ToString();
			config["normDec"] = options.normDec.ToString();
			config["radiInc"] = options.radiInc.ToString();
			config["radiDec"] = options.radiDec.ToString();
			config["timeInc"] = options.timeInc.ToString();
			config["timeDec"] = options.timeDec.ToString();
			config["pageIncrement"] = options.pageIncrement.ToString();
			config["pageConics"] = options.pageConics.ToString();
			config["hideWindow"] = options.hideWindow.ToString();
			config["addWidget"] = options.addWidget.ToString();
			config["mainWindowX"] = (int)options.mainWindowPos.x;
			config["mainWindowY"] = (int)options.mainWindowPos.y;
			config["optWindowX"] = (int)options.optionsWindowPos.x;
			config["optWindowY"] = (int)options.optionsWindowPos.y;
			config["keyWindowX"] = (int)options.keymapperWindowPos.x;
			config["keyWindowY"] = (int)options.keymapperWindowPos.y;
			config["clockWindowX"] = (int)options.clockWindowPos.x;
			config["clockWindowY"] = (int)options.clockWindowPos.y;
			config["conicsWindowX"] = (int)options.conicsWindowPos.x;
			config["conicsWindowY"] = (int)options.conicsWindowPos.y;
			config["tripWindowX"] = (int)options.tripWindowPos.x;
			config["tripWindowY"] = (int)options.tripWindowPos.y;
			config["showClock"] = options.showClock;
			config["showEAngle"] = options.showEAngle;
			config["showConicsAlways"] = options.showConicsAlways;
			config["showOrbitInfo"] = options.showOrbitInfo;
			config["showUTControls"] = options.showUTControls;
			config["showManeuverPager"] = options.showManeuverPager;

			config.save();
		}

		private enum KEYS : byte {
			PROGINC,
			PROGDEC,
			NORMINC,
			NORMDEC,
			RADIINC,
			RADIDEC,
			TIMEINC,
			TIMEDEC,
			PAGEINC,
			PAGECON,
			HIDEWINDOW,
			ADDWIDGET
		};
	}	
}

