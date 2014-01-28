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
	[KSPAddon(KSPAddon.Startup.Flight, false)]

	public class PreciseNode : MonoBehaviour {
		public PluginConfiguration config;
		private PNOptions options = new PNOptions();
		private NodeManager curState = new NodeManager();
		private List<Action> scheduledForLayout = new List<Action>();

		private bool configLoaded = false;
		private bool conicsLoaded = false;
		private bool shown = true;
		private bool showTimeNext = false;
		private bool waitForKey = false;
		private bool showOptions = false;
		private bool showKeymapper = false;
		private bool showEncounter = false;
		private byte currentWaitKey = 255;
		private double keyWaitTime = 0.0;

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
			if(canShowNodeEditor && !FlightDriver.Pause) {
				PatchedConicSolver solver = NodeTools.getSolver();
				if(solver.maneuverNodes.Count > 0) {
					if(!curState.hasNode() || !solver.maneuverNodes.Contains(curState.node)) {
						// get the first one if we can't find the current or it's null
						curState = new NodeManager(solver.maneuverNodes[0]);
					} else if(curState.hasNode()) {
						curState.updateNode();
						curState = curState.nextState();
					}
				} else {
					if(curState.hasNode()) {
						curState = new NodeManager();
						curState.resizeClockWindow = true;
					}
				}
				processKeyInput();
			}
		}

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		public void OnGUI() {
			// Porcess any scheduled functions
			if(Event.current.type == EventType.Layout && !FlightDriver.Pause && scheduledForLayout.Count > 0) {
				foreach(Action a in scheduledForLayout) {
					a();
				}
				scheduledForLayout.Clear();
			}
			if(canShowNodeEditor) {
				if(Event.current.type == EventType.Layout && !FlightDriver.Pause) {
					// On layout we should see if we have nodes to act on.
					if(curState.resizeMainWindow) {
						options.mainWindowPos.height = 250;
					}
					if(curState.resizeClockWindow) {
						options.clockWindowPos.height = 65;
					}
					showEncounter = curState.encounter;
					// this prevents the clock window from showing the time to
					// next node when the next state is created during repaint.
					showTimeNext = curState.hasNode();
				}
				if(!conicsLoaded) {
					NodeTools.changeConicsMode(options.conicsMode);
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
			Color contentColor = GUI.contentColor;
			Color curColor = defaultColor;
			PatchedConicSolver solver = NodeTools.getSolver();

			// Options button
			if(showOptions) { GUI.backgroundColor = Color.green; }
			if(GUI.Button(new Rect(options.mainWindowPos.width - 48, 2, 22, 18), "O")) {
				showOptions = !showOptions;
			}
			GUI.backgroundColor = defaultColor;

			// Keymapping button
			if(showKeymapper) { GUI.backgroundColor = Color.green; }
			if(GUI.Button(new Rect(options.mainWindowPos.width - 24, 2, 22, 18), "K")) {
				showKeymapper = !showKeymapper;
			}
			GUI.backgroundColor = defaultColor;

			GUILayout.BeginVertical();
			if(options.showManeuverPager) {
				GUIParts.drawManeuverPager(curState);
			}

			// Human-readable time
			GUIParts.drawDoubleLabel("Time:", 100, NodeTools.convertUTtoHumanTime(curState.currentUT()), 130);

			// Increment buttons
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment:", GUILayout.Width(100));
			GUIParts.drawButton("0.01", (options.increment == 0.01?Color.yellow:defaultColor), delegate() { options.increment = 0.01; });
			GUIParts.drawButton("0.1", (options.increment == 0.1?Color.yellow:defaultColor), delegate() { options.increment = 0.1; });
			GUIParts.drawButton("1", (options.increment == 1?Color.yellow:defaultColor), delegate() { options.increment = 1; });
			GUIParts.drawButton("10", (options.increment == 10?Color.yellow:defaultColor), delegate() { options.increment = 10; });
			GUIParts.drawButton("100", (options.increment == 100?Color.yellow:defaultColor), delegate() { options.increment = 100; });
			GUILayout.EndHorizontal();

			drawTimeControls(contentColor);
			drawProgradeControls(contentColor);
			drawNormalControls(contentColor);
			drawRadialControls(contentColor);

			// total delta-V display
			GUIParts.drawDoubleLabel("Total delta-V:", 100, curState.currentMagnitude().ToString("0.##") + "m/s", 130);

			drawEAngle();
			drawEncounter(defaultColor);

			// Conics mode controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Conics mode: ", GUILayout.Width(100));
			GUIParts.drawButton("0", (options.conicsMode == 0?Color.yellow:defaultColor), delegate() { options.setConicsMode(0); });
			GUIParts.drawButton("1", (options.conicsMode == 1?Color.yellow:defaultColor), delegate() { options.setConicsMode(1); });
			GUIParts.drawButton("2", (options.conicsMode == 2?Color.yellow:defaultColor), delegate() { options.setConicsMode(2); });
			GUIParts.drawButton("3", (options.conicsMode == 3?Color.yellow:defaultColor), delegate() { options.setConicsMode(3); });
			GUIParts.drawButton("4", (options.conicsMode == 4?Color.yellow:defaultColor), delegate() { options.setConicsMode(4); });
			GUILayout.EndHorizontal();
			
			// conics patch limit editor.
			GUILayout.BeginHorizontal();
			GUILayout.Label("Change Conics Samples", GUILayout.Width(200));
			GUIParts.drawButton("-", Color.red, delegate() { solver.DecreasePatchLimit(); });
			GUIParts.drawButton("+", Color.red, delegate() { solver.IncreasePatchLimit(); });
			GUILayout.EndHorizontal();
			
			// trip info button and vessel focus buttons
			GUILayout.BeginHorizontal();
			GUIParts.drawButton("Trip Info", (options.showTrip?Color.yellow:defaultColor), delegate() { options.showTrip = !options.showTrip; });
			GUIParts.drawButton("Focus on Vessel", defaultColor, delegate() { MapView.MapCamera.SetTarget(FlightGlobals.ActiveVessel.vesselName); });
			GUILayout.EndHorizontal();
			
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		// debugging function
		private void drawEAngle() {
			// Ejection angle
			if(options.showEAngle) {
				String eangle = "n/a";
				if(FlightGlobals.ActiveVessel.orbit.referenceBody.name != "Sun") {
					eangle = NodeTools.getEjectionAngle(FlightGlobals.ActiveVessel.orbit, curState.currentUT()).ToString("0.##") + "°";
				}
				GUIParts.drawDoubleLabel("Ejection Angle:", 100, eangle, 130);
			}
		}

		// debugging function
		private void drawEncounter(Color defaultColor) {
			// Additional Information
			if(options.showOrbitInfo) {
				// Find the next encounter, if any, in our flight plan.
				if(showEncounter) {
					Orbit nextEnc = NodeTools.findNextEncounter(curState.node);
					string name = "N/A";
					string PeA = "N/A";

					if(nextEnc != null) {
						name = nextEnc.referenceBody.name;
						PeA = NodeTools.formatMeters(nextEnc.PeA);
					} else {
						curState.encounter = false;
					}
					// Next encounter periapsis
					GUIParts.drawDoubleLabel("(" + name + ") Pe:", 100, PeA, 130);
					GUILayout.BeginHorizontal();
					GUILayout.Label("", GUILayout.Width(100));
					GUIParts.drawButton("Focus on " + name, defaultColor, delegate() { MapView.MapCamera.SetTarget(name); });
					GUILayout.EndHorizontal();
				} else {
					if(curState.node.solver.flightPlan.Count > 1) {
						// output the apoapsis and periapsis of our projected orbit.
						GUIParts.drawDoubleLabel("Apoapsis:", 100, NodeTools.formatMeters(curState.node.nextPatch.ApA), 100);
						GUIParts.drawDoubleLabel("Periapsis:", 100, NodeTools.formatMeters(curState.node.nextPatch.PeA), 130);
					}
				}
			}
		}

		// debugging function
		private void drawTimeControls(Color contentColor) {
			// Universal time controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("UT:", GUILayout.Width(100));
			GUI.backgroundColor = Color.green;
			if(!curState.timeParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.timeText, GUILayout.Width(100));
			if(!curState.timeText.Equals(check, StringComparison.Ordinal)) {
				curState.setUT(check);
			}
			GUI.contentColor = contentColor;
			GUIParts.drawButton("-", Color.red, delegate() { curState.addUT(options.increment * -1.0); });
			GUIParts.drawButton("+", Color.green, delegate() { curState.addUT(options.increment); });
			GUILayout.EndHorizontal();

			// extended time controls
			if(options.showUTControls) {
				GUILayout.BeginHorizontal();
				GUIParts.drawButton("Peri", Color.yellow, delegate() { curState.setUT(Planetarium.GetUniversalTime() + curState.node.patch.timeToPe); });
				GUIParts.drawButton("DN", Color.magenta, delegate() {
					Orbit targ = NodeTools.getTargetOrbit();
					if(targ != null) {
						curState.setUT(NodeTools.getTargetDNUT(curState.node.patch, targ));
					} else {
						curState.setUT(NodeTools.getEquatorialDNUT(curState.node.patch));
					}
				});
				GUIParts.drawButton("-1K", Color.red, delegate() { curState.addUT(-1000); });
				GUIParts.drawButton("+1K", Color.green, delegate() { curState.addUT(1000); });
				GUIParts.drawButton("AN", Color.cyan, delegate() {
					Orbit targ = NodeTools.getTargetOrbit();
					if(targ != null) {
						curState.setUT(NodeTools.getTargetANUT(curState.node.patch, targ));
					} else {
						curState.setUT(NodeTools.getEquatorialANUT(curState.node.patch));
					}
				});
				GUIParts.drawButton("Apo", Color.blue, delegate() { curState.setUT(Planetarium.GetUniversalTime() + curState.node.patch.timeToAp); });
				GUILayout.EndHorizontal();
			}
		}

		// debugging function
		private void drawProgradeControls(Color contentColor) {
			// Prograde controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Prograde:", GUILayout.Width(100));
			if(!curState.progradeParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.progradeText, GUILayout.Width(100));
			if(!curState.progradeText.Equals(check, StringComparison.Ordinal)) {
				curState.setPrograde(check);
			}
			GUI.contentColor = contentColor;
			GUIParts.drawButton("-", Color.red, delegate() { curState.addPrograde(options.increment * -1.0); });
			GUIParts.drawButton("+", Color.green, delegate() { curState.addPrograde(options.increment); });
			GUILayout.EndHorizontal();
		}

		// debugging function
		private void drawNormalControls(Color contentColor) {
			// Normal controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Normal:", GUILayout.Width(100));
			if(!curState.normalParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.normalText, GUILayout.Width(100));
			if(!curState.normalText.Equals(check, StringComparison.Ordinal)) {
				curState.setNormal(check);
			}
			GUI.contentColor = contentColor;
			GUIParts.drawButton("-", Color.red, delegate() { curState.addNormal(options.increment * -1.0); });
			GUIParts.drawButton("+", Color.green, delegate() { curState.addNormal(options.increment); });
			GUILayout.EndHorizontal();
		}

		// debugging function
		private void drawRadialControls(Color contentColor) {
			// radial controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Radial:", GUILayout.Width(100));
			if(!curState.radialParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.radialText, GUILayout.Width(100));
			if(!curState.radialText.Equals(check, StringComparison.Ordinal)) {
				curState.setRadial(check);
			}
			GUI.contentColor = contentColor;
			GUIParts.drawButton("-", Color.red, delegate() { curState.addRadial(options.increment * -1.0); });
			GUIParts.drawButton("+", Color.green, delegate() { curState.addRadial(options.increment); });
			GUILayout.EndHorizontal();
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

			GUIParts.drawDoubleLabel("Time:", 35, timeHuman, 150);
			GUIParts.drawDoubleLabel("UT:", 35, Math.Floor(timeNow).ToString("F0"), 150);

			if(showTimeNext) {
				double next = 0.0;
				string labelText = "";
				if(NodeTools.getSolver().maneuverNodes.Count > 0) {
					// protection from index out of range errors.
					// should probably handle this better.
					next = timeNow - NodeTools.getSolver().maneuverNodes[0].UT;
				}
				if(next < 0) {
					labelText = "T- " + NodeTools.convertUTtoHumanDuration(next);
				} else {
					labelText = "T+ " + NodeTools.convertUTtoHumanDuration(next);
				}
				GUIParts.drawDoubleLabel("Next:", 35, labelText, 150);
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
			GUIParts.drawButton("0", (options.conicsMode == 0?Color.yellow:defaultColor), delegate() { options.setConicsMode(0); });
			GUIParts.drawButton("1", (options.conicsMode == 1?Color.yellow:defaultColor), delegate() { options.setConicsMode(1); });
			GUIParts.drawButton("2", (options.conicsMode == 2?Color.yellow:defaultColor), delegate() { options.setConicsMode(2); });
			GUIParts.drawButton("3", (options.conicsMode == 3?Color.yellow:defaultColor), delegate() { options.setConicsMode(3); });
			GUIParts.drawButton("4", (options.conicsMode == 4?Color.yellow:defaultColor), delegate() { options.setConicsMode(4); });
			GUILayout.EndHorizontal();

			// conics patch limit editor.
			GUILayout.BeginHorizontal();
			GUILayout.Label("Change Conics Samples", GUILayout.Width(200));
			GUIParts.drawButton("-", Color.red, delegate() { solver.DecreasePatchLimit(); });
			GUIParts.drawButton("+", Color.red, delegate() { solver.IncreasePatchLimit(); });
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
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Hide/Show Window...", (byte)KEYS.HIDEWINDOW); });
			GUILayout.EndHorizontal();

			// Set add node widget button
			GUILayout.BeginHorizontal();
			GUILayout.Label("Open Node Gizmo: " + options.addWidget.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Open Node Gizmo...", (byte)KEYS.ADDWIDGET); });
			GUILayout.EndHorizontal();

			// Set prograde controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Prograde: " + options.progInc.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Increment Prograde...", (byte)KEYS.PROGINC); });
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Prograde: " + options.progDec.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Decrement Prograde...", (byte)KEYS.PROGDEC); });
			GUILayout.EndHorizontal();

			// set normal controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Normal: " + options.normInc.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Increment Normal...", (byte)KEYS.NORMINC); });
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Normal: " + options.normDec.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Decrement Normal...", (byte)KEYS.NORMDEC); });
			GUILayout.EndHorizontal();

			// set radial controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Radial: " + options.radiInc.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Increment Radial...", (byte)KEYS.RADIINC); });
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Radial: " + options.radiDec.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Decrement Radial...", (byte)KEYS.RADIDEC); });
			GUILayout.EndHorizontal();

			// set time controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Time: " + options.timeInc.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Increment Time...", (byte)KEYS.TIMEINC); });
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Time: " + options.timeDec.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Decrement Time...", (byte)KEYS.TIMEDEC); });
			GUILayout.EndHorizontal();

			// set paging controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Page Increment: " + options.pageIncrement.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Page Increment...", (byte)KEYS.PAGEINC); });
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Page Conics: " + options.pageConics.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("set", defaultColor, delegate() { doWaitForKey("Press a key to bind Page Conics Mode...", (byte)KEYS.PAGECON); });
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
				GUILayout.Label("Time Until", GUILayout.Width(200));
				GUILayout.Label("", GUILayout.Width(120));
				GUILayout.EndHorizontal();

				foreach(ManeuverNode curNode in solver.maneuverNodes) {
					int idx = solver.maneuverNodes.IndexOf(curNode);
					double timeDiff = curNode.UT - timeNow;
					GUILayout.BeginHorizontal();
					GUILayout.Label("Node " + idx, GUILayout.Width(60));
					GUILayout.Label(curNode.DeltaV.magnitude.ToString("F2") + "m/s", GUILayout.Width(90));
					GUILayout.Label(NodeTools.convertUTtoHumanDuration(timeDiff), GUILayout.Width(200));
					// these will be scheduled for during the next layout pass
					if(idx > 0) {
						GUIParts.drawButton("merge ▲", Color.white, delegate() {scheduledForLayout.Add(new Action(() => {NodeTools.mergeNodeDown(solver.maneuverNodes[idx]);}));});
					}
					GUILayout.EndHorizontal();
					total += curNode.DeltaV.magnitude;
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("Total", GUILayout.Width(60));
				GUILayout.Label(total.ToString("F2") + "m/s", GUILayout.Width(90));
				GUILayout.Label("", GUILayout.Width(200));
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
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled && NodeTools.getSolver().maneuverNodes.Count > 0;
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


		private void doWaitForKey(String msg, byte key) {
			ScreenMessages.PostScreenMessage(msg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
			waitForKey = true;
			currentWaitKey = key;
			keyWaitTime = Planetarium.GetUniversalTime();
		}

		/// <summary>
		/// Processes keyboard input.
		/// </summary>
		private void processKeyInput() {
			if(!Input.anyKeyDown) {
				return;
			}

			// Fix for a bug in Linux where typing would still control game elements even if
			// a textbox was focused.
			if(GUIUtility.keyboardControl != 0) {
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
					options.downIncrement();
				} else {
					options.upIncrement();
				}
			}
			// prograde increment
			if(Input.GetKeyDown(options.progInc)) {
				curState.addPrograde(options.increment);
			}
			// prograde decrement
			if(Input.GetKeyDown(options.progDec)) {
				curState.addPrograde(options.increment * -1.0);
			}
			// normal increment
			if(Input.GetKeyDown(options.normInc)) {
				curState.addNormal(options.increment);
			}
			// normal decrement
			if(Input.GetKeyDown(options.normDec)) {
				curState.addNormal(options.increment * -1.0);
			}
			// radial increment
			if(Input.GetKeyDown(options.radiInc)) {
				curState.addRadial(options.increment);
			}
			// radial decrement
			if(Input.GetKeyDown(options.radiDec)) {
				curState.addRadial(options.increment * -1.0);
			}
			// UT increment
			if(Input.GetKeyDown(options.timeInc)) {
				curState.addUT(options.increment);
			}
			// UT decrement
			if(Input.GetKeyDown(options.timeDec)) {
				curState.addUT(options.increment * -1.0);
			}
			// Page Conics
			if(Input.GetKeyDown(options.pageConics)) {
				options.pageConicsMode();
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
					options.conicsMode = config.GetValue<int>("conicsMode", 3);
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

			config["conicsMode"] = options.conicsMode;
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

