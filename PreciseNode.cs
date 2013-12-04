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

		private NodeTools tools = new NodeTools();
		private ManeuverNode node = null;
		private Rect mainWindowPos = new Rect(Screen.width / 10, 20, 250, 130);
		private Rect optionsWindowPos = new Rect(Screen.width / 3, 20, 250, 130);
		private Rect keymapperWindowPos = new Rect(Screen.width / 5, 20, 250, 130);
		private Rect clockWindowPos = new Rect(Screen.width / 3, Screen.height / 2, 195, 65);
		private Rect conicsWindowPos = new Rect(Screen.width / 5, Screen.height / 2, 250, 65);
		private Rect tripWindowPos = new Rect(Screen.width / 5, Screen.height / 5, 320, 65);

		private bool configLoaded = false;
		private bool showOptions = false;
		private bool showKeymapper = false;
		private bool showManeuverPager = true;
		private bool showConicsAlways = false;
		private bool showClock = false;
		private bool showTrip = false;
		private bool showUTControls = false;
		private bool showEAngle = true;
		private bool showOrbitInfo = false;
		private bool conicsLoaded = false;
		private bool shown = true;
        private bool waitForKey = false;
		private bool nodeChanged = false;
        private byte currentWaitKey = 255;
		private int conicsMode = 3;
        private double keyWaitTime = 0.0;
		private double lastUT = 0;
		private double increment = 1.0;

		private KeyCode progInc = KeyCode.Keypad8;
		private KeyCode progDec = KeyCode.Keypad5;
		private KeyCode normInc = KeyCode.Keypad9;
		private KeyCode normDec = KeyCode.Keypad7;
		private KeyCode radiInc = KeyCode.Keypad6;
		private KeyCode radiDec = KeyCode.Keypad4;
		private KeyCode timeInc = KeyCode.Keypad3;
		private KeyCode timeDec = KeyCode.Keypad1;
		private KeyCode pageIncrement = KeyCode.Keypad0;
		private KeyCode pageConics = KeyCode.KeypadEnter;
		private KeyCode hideWindow = KeyCode.P;
		private KeyCode addWidget = KeyCode.O;

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
			PatchedConicSolver solver = tools.getSolver();
			if(solver.maneuverNodes.Count > 0) {
				if(node == null || !solver.maneuverNodes.Contains(node)) {
					// get the first one if we can't find the current or it's null
					node = solver.maneuverNodes[0];
					nodeChanged = true;
				}
				lastUT = node.UT;
			} else {
				if(lastUT != 0) {
					node = null;
					nodeChanged = true;
					lastUT = 0;
				}
			}
			if(canShowNodeEditor) {
				processKeyInput();
			}
		}
		
        /// <summary>
        /// Overridden function from MonoBehavior
        /// </summary>
		public void OnGUI() {
			if(canShowNodeEditor) {
				if(!conicsLoaded) {
					tools.changeConicsMode(conicsMode);
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
			mainWindowPos = GUILayout.Window(21349, mainWindowPos, drawMainWindow, "Precise Node", GUILayout.ExpandHeight(true));
            if(showOptions) {
				optionsWindowPos = GUILayout.Window(21350, optionsWindowPos, drawOptionsWindow, "Precise Node Options", GUILayout.ExpandHeight(true));
            }
			if(showKeymapper) {
				keymapperWindowPos = GUILayout.Window(21351, keymapperWindowPos, drawKeymapperWindow, "Precise Node Keys", GUILayout.ExpandHeight(true));
			}
			if(showTrip) {
				tripWindowPos = GUILayout.Window(21352, tripWindowPos, drawTripWindow, "Trip Info", GUILayout.ExpandHeight(true));
			}
		}

        /// <summary>
        /// Draw Clock GUI
        /// </summary>
        public void drawClockGUI() {
			GUI.skin = null;
            clockWindowPos = GUILayout.Window(21353, clockWindowPos, drawClockWindow, "Clock", GUILayout.ExpandHeight(true));
        }
		
        /// <summary>
        /// Draw Conics GUI
        /// </summary>
        public void drawConicsGUI() {
			GUI.skin = null;
            conicsWindowPos = GUILayout.Window(21354, conicsWindowPos, drawConicsWindow, "Conics Controls", GUILayout.ExpandHeight(true));
        }
		
        /// <summary>
        /// Draws the Node Editor window.
        /// </summary>
        /// <param name="id">Identifier.</param>
		public void drawMainWindow(int id) {
			Color defaultColor = GUI.backgroundColor;
			double convertedTime = 0;
			bool changed = false;
			PatchedConicSolver solver = tools.getSolver();

			String timeHuman = tools.convertUTtoHumanTime(lastUT);
			String timeUT = lastUT.ToString("0.##");
			String prograde = node.DeltaV.z.ToString("0.##") + "m/s";
			String normal = node.DeltaV.y.ToString("0.##") + "m/s";
			String radial = node.DeltaV.x.ToString("0.##") + "m/s";
			String eangle = "n/a";
			if(FlightGlobals.ActiveVessel.orbit.referenceBody.name != "Sun") {
				eangle = tools.getEjectionAngle(FlightGlobals.ActiveVessel.orbit, node.UT).ToString("0.##") + "°";
			}

			// Options button
			if(GUI.Button(new Rect(mainWindowPos.width - 48, 2, 22, 18), "O")) {
				showOptions = true;
			}
            // Keymapping button
			if(GUI.Button(new Rect(mainWindowPos.width - 24, 2, 22, 18), "K")) {
                showKeymapper = true;
            }

			GUILayout.BeginVertical();

			if(showManeuverPager) {
	            // Maneuver node paging controls
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("<")) {
					int count = solver.maneuverNodes.Count;
					if(count > 1) {
						if(node == null) {
							// get the first node.
							node = solver.maneuverNodes[0];
							lastUT = node.UT;
						} else {
							// get the previous or last node
							int idx = solver.maneuverNodes.IndexOf(node);
							if(idx == 0) {
								node = solver.maneuverNodes[--count];
							} else {
								node = solver.maneuverNodes[--idx];
							}
							lastUT = node.UT;
							return;
						}
					} else {
						return;
					}
				}
				if(GUILayout.Button("Editing Node " + (solver.maneuverNodes.IndexOf(node) + 1))) {
					MapView.MapCamera.SetTarget(node.scaledSpaceTarget);
				}
				// GUILayout.Label("Editing Node " + (solver.maneuverNodes.IndexOf(node) + 1), GUILayout.Width(100));
				if(GUILayout.Button(">")) {
					int count = solver.maneuverNodes.Count;
					if(count > 1) {
						if(node == null) {
							// get the first node.
							node = solver.maneuverNodes[0];
							lastUT = node.UT;
						} else {
							// get the previous or last node
							int idx = solver.maneuverNodes.IndexOf(node);
							if(idx == count - 1) {
								node = solver.maneuverNodes[0];
							} else {
								node = solver.maneuverNodes[++idx];
							}
							lastUT = node.UT;
							return;
						}
					} else {
						return;
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
			String checkTime = GUILayout.TextField(timeUT, GUILayout.Width(100));
			try {
				convertedTime = Convert.ToDouble(checkTime);
			} catch(FormatException) {
				convertedTime = lastUT;
			}
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				convertedTime -= increment;
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				convertedTime += increment;
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

			if(showUTControls) {
				GUILayout.BeginHorizontal();
				GUI.backgroundColor = Color.yellow;
				if(GUILayout.Button("Peri")) {
					convertedTime = Planetarium.GetUniversalTime() + node.patch.timeToPe;
				}
				GUI.backgroundColor = Color.magenta;
				if(GUILayout.Button("-10K")) {
					convertedTime -= 10000;
				}
				GUI.backgroundColor = Color.red;
				if(GUILayout.Button("-1K")) {
					convertedTime -= 1000;
				}
				GUI.backgroundColor = Color.green;
				if(GUILayout.Button("+1K")) {
					convertedTime += 1000;
				}
				GUI.backgroundColor = Color.cyan;
				if(GUILayout.Button("+10K")) {
					convertedTime += 10000;
				}
				GUI.backgroundColor = Color.blue;
				if(GUILayout.Button("Apo")) {
					convertedTime = Planetarium.GetUniversalTime() + node.patch.timeToAp;
				}
				GUI.backgroundColor = defaultColor;
				GUILayout.EndHorizontal();
			}

			if(convertedTime != node.UT) {
				node.UT = convertedTime;
				node.attachedGizmo.UT = convertedTime;
				changed = true;
			}

            // Prograde controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Prograde:", GUILayout.Width(100));
			GUILayout.Label(prograde, GUILayout.Width(100));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				node.DeltaV.z -= increment;
				node.attachedGizmo.DeltaV.z -= increment;
				changed = true;
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				node.DeltaV.z += increment;
				node.attachedGizmo.DeltaV.z += increment;
				changed = true;
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

            // Normal controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Normal:", GUILayout.Width(100));
			GUILayout.Label(normal, GUILayout.Width(100));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				node.DeltaV.y -= increment;
				node.attachedGizmo.DeltaV.y -= increment;
				changed = true;
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				node.DeltaV.y += increment;
				node.attachedGizmo.DeltaV.y += increment;
				changed = true;
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

            // radial controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Radial:", GUILayout.Width(100));
			GUILayout.Label(radial, GUILayout.Width(100));
			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("-")) {
				node.DeltaV.x -= increment;
				node.attachedGizmo.DeltaV.x -= increment;
				changed = true;
			}
			GUI.backgroundColor = Color.green;
			if(GUILayout.Button("+")) {
				node.DeltaV.x += increment;
				node.attachedGizmo.DeltaV.x += increment;
				changed = true;
			}
			GUI.backgroundColor = defaultColor;
			GUILayout.EndHorizontal();

            // total delta-V display
			GUILayout.BeginHorizontal();
			GUILayout.Label("Total delta-V:",GUILayout.Width(100));
			GUILayout.Label(node.DeltaV.magnitude.ToString("0.##") + "m/s");
			GUILayout.EndHorizontal();

			// Ejection angle
			if(showEAngle) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Ejection Angle:", GUILayout.Width(100));
				GUILayout.Label(eangle, GUILayout.Width(100));
				GUILayout.EndHorizontal();
			}

			// Additional Information
			if(showOrbitInfo) {
				// Find the next encounter, if any, in our flight plan.
				Orbit nextEnc = tools.findNextEncounter(node);
				if(nextEnc == null) {
					if(node.solver.flightPlan.Count > 1) {
						// output the apoapsis and periapsis of our projected orbit.
						GUILayout.BeginHorizontal();
						GUILayout.Label("Apoapsis:", GUILayout.Width(100));
						// GUILayout.Label(tools.formatMeters(plan[1].ApA), GUILayout.Width(100));
						GUILayout.Label(tools.formatMeters(node.patch.ApA), GUILayout.Width(100));
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						GUILayout.Label("Periapsis:", GUILayout.Width(100));
						// GUILayout.Label(tools.formatMeters(plan[1].PeA), GUILayout.Width(100));
						GUILayout.Label(tools.formatMeters(node.patch.PeA), GUILayout.Width(100));
						GUILayout.EndHorizontal();
					}
				} else {
					// Next encounter periapsis
					GUILayout.BeginHorizontal();
					GUILayout.Label("(" + nextEnc.referenceBody.name + ") Pe:", GUILayout.Width(100));
					GUILayout.Label(tools.formatMeters(nextEnc.PeA), GUILayout.Width(100));
					GUILayout.EndHorizontal();
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
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 1) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("1")) {
				conicsMode = 1;
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 2) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("2")) {
				conicsMode = 2;
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 3) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("3")) {
				conicsMode = 3;
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 4) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("4")) {
				conicsMode = 4;
				tools.changeConicsMode(conicsMode);
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
			GUILayout.Label("", GUILayout.Width(100));
			if(showTrip) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("Trip Info")) {
				showTrip = !showTrip;
			}
			GUILayout.EndHorizontal();
	
			GUILayout.EndVertical();
			GUI.DragWindow();

			if(changed) {
				node.solver.UpdateFlightPlan();
			}
		}

		/// <summary>
		/// Draws the Clock window.
		/// </summary>
		/// <param name="id">Identifier.</param>
        public void drawClockWindow(int id) {
			Color defaultColor = GUI.backgroundColor;
            double timeNow = Planetarium.GetUniversalTime();
			String timeUT = timeNow.ToString("F0");
			String timeHuman = tools.convertUTtoHumanTime(timeNow);

			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Time:", GUILayout.Width(35));
			GUILayout.Label(timeHuman, GUILayout.Width(150));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("UT:", GUILayout.Width(35));
			GUILayout.Label(Math.Floor(timeNow).ToString("F0"), GUILayout.Width(150));
			GUILayout.EndHorizontal();

			if(node != null) {
				double next = timeNow - tools.getSolver().maneuverNodes[0].UT;
				GUILayout.BeginHorizontal();
				GUILayout.Label("Next:", GUILayout.Width(35));
				if(next < 0) {
					GUILayout.Label("T- " + tools.convertUTtoHumanDuration(next), GUILayout.Width(150));
				} else {
					GUILayout.Label("T+ " + tools.convertUTtoHumanDuration(next), GUILayout.Width(150));
				}
				GUILayout.EndHorizontal();
			}
			if(nodeChanged) {
				clockWindowPos.height = 65;
				nodeChanged = false;
			}

			GUILayout.EndVertical();
			GUI.DragWindow();
        }

		/// <summary>
		/// Draws the Conics window.
		/// </summary>
		/// <param name="id">Identifier.</param>
        public void drawConicsWindow(int id) {
			PatchedConicSolver solver = tools.getSolver();
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
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 1) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("1")) {
				conicsMode = 1;
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 2) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("2")) {
				conicsMode = 2;
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 3) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("3")) {
				conicsMode = 3;
				tools.changeConicsMode(conicsMode);
			}
			GUI.backgroundColor = defaultColor;
			if(conicsMode == 4) {
				GUI.backgroundColor = Color.yellow;
			}
			if(GUILayout.Button("4")) {
				conicsMode = 4;
				tools.changeConicsMode(conicsMode);
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
			if(GUI.Button(new Rect(optionsWindowPos.width - 24, 2, 22, 18), "X")) {
				showOptions = false;
			}

			GUILayout.BeginVertical();
			showConicsAlways = GUILayout.Toggle(showConicsAlways, "Always Show Conics Controls");
			showClock = GUILayout.Toggle(showClock, "Show Clock");
			// use a temp variable so we can check whether the main window needs resizing.
			bool temp = GUILayout.Toggle(showManeuverPager, "Show Maneuver Pager");
			if(temp != showManeuverPager) {
				showManeuverPager = temp;
				mainWindowPos.height = 250;
			}
			temp = GUILayout.Toggle(showUTControls, "Show Additional UT Controls");
			if(temp != showUTControls) {
				showUTControls = temp;
				mainWindowPos.height = 250;
			}
			temp = GUILayout.Toggle(showEAngle, "Show Ejection Angle");
			if(temp != showEAngle) {
				showEAngle = temp;
				mainWindowPos.height = 250;
			}
			temp = GUILayout.Toggle(showOrbitInfo, "Show Orbit Information");
			if(temp != showOrbitInfo) {
				showOrbitInfo = temp;
				mainWindowPos.height = 250;
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
			if(GUI.Button(new Rect(keymapperWindowPos.width - 24, 2, 22, 18), "X")) {
                showKeymapper = false;
            }

			GUILayout.BeginVertical();

			// Set window control
			GUILayout.BeginHorizontal();
			GUILayout.Label("Hide/Show Window: " + hideWindow.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Hide/Show Window...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.HIDEWINDOW;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			// Set add node widget button
			GUILayout.BeginHorizontal();
			GUILayout.Label("Open Node Gizmo: " + addWidget.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
				ScreenMessages.PostScreenMessage("Press a key to bind Open Node Gizmo...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				waitForKey = true;
				currentWaitKey = (byte)KEYS.ADDWIDGET;
				keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

            // Set prograde controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Prograde: " + progInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Increment Prograde...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.PROGINC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Prograde: " + progDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Decrement Prograde...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.PROGDEC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

            // set normal controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Normal: " + normInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Increment Normal...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.NORMINC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Normal: " + normDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Decrement Normal...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.NORMDEC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

            // set radial controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Radial: " + radiInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Increment Radial...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.RADIINC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Radial: " + radiDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Decrement Radial...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.RADIDEC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

            // set time controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment Time: " + timeInc.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Increment Time...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.TIMEINC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Decrement Time: " + timeDec.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Decrement Time...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.TIMEDEC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

            // set paging controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Page Increment: " + pageIncrement.ToString(), GUILayout.Width(200));
			if(GUILayout.Button("set")) {
                ScreenMessages.PostScreenMessage("Press a key to bind Page Increment...", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                waitForKey = true;
				currentWaitKey = (byte)KEYS.PAGEINC;
                keyWaitTime = Planetarium.GetUniversalTime();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Page Conics: " + pageConics.ToString(), GUILayout.Width(200));
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
			PatchedConicSolver solver = tools.getSolver();

			GUILayout.BeginVertical();
			if(solver.maneuverNodes.Count < 1) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("No nodes to show.", GUILayout.Width(200));
				GUILayout.EndHorizontal();
			} else {
				double d = 0.0;
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
					GUILayout.Label(tools.convertUTtoHumanDuration(timeDiff), GUILayout.Width(150));
					GUILayout.EndHorizontal();
					d += curNode.DeltaV.magnitude;
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("", GUILayout.Width(60));
				GUILayout.Label(d.ToString("F2") + "m/s", GUILayout.Width(90));
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
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled && node != null;
			}
		}

        /// <summary>
        /// Returns whether the Conics Window can be shown based on a number of global factors.
        /// </summary>
        /// <value><c>true</c> if the Conics Window can be shown; otherwise, <c>false</c>.</value>
		private bool canShowConicsWindow {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled && showConicsAlways;
			}
		}

        /// <summary>
        /// Returns whether the Clock Window can be shown based on a number of global factors.
        /// </summary>
        /// <value><c>true</c> if the Clock Window can be shown; otherwise, <c>false</c>.</value>
		private bool canShowClock {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && RenderingManager.fetch.enabled && showClock;
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
				KeyCode key = tools.fetchKey();
                // if the time is up or we have no key to process, reset.
                if(((keyWaitTime + 5.0) < Planetarium.GetUniversalTime()) || key == KeyCode.None) {
					Debug.Log("Outside wait time.");
                    currentWaitKey = 255;
                    waitForKey = false;
                    return;
                }

                // which key are we waiting for?
                switch(currentWaitKey) {
					case (byte)KEYS.PROGINC:
                        progInc = key;
                        break;
					case (byte)KEYS.PROGDEC:
                        progDec = key;
                        break;
					case (byte)KEYS.NORMINC:
                        normInc = key;
                        break;
					case (byte)KEYS.NORMDEC:
                        normDec = key;
                        break;
					case (byte)KEYS.RADIINC:
                        radiInc = key;
                        break;
					case (byte)KEYS.RADIDEC:
                        radiDec = key;
                        break;
					case (byte)KEYS.TIMEINC:
                        timeInc = key;
                        break;
					case (byte)KEYS.TIMEDEC:
                        timeDec = key;
                        break;
					case (byte)KEYS.PAGEINC:
                        pageIncrement = key;
                        break;
                    case (byte)KEYS.PAGECON:
                        pageConics = key;
                        break;
					case (byte)KEYS.HIDEWINDOW:
						hideWindow = key;
						break;
					case (byte)KEYS.ADDWIDGET:
						addWidget = key;
						break;
                }
                currentWaitKey = 255;
                waitForKey = false;
                return;
            }

            // process normal keyboard input
			bool changed = false;
            // change increment
			if(Input.GetKeyDown(pageIncrement)) {
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
			if(Input.GetKeyDown(progInc)) {
				node.DeltaV.z += increment;
				node.attachedGizmo.DeltaV.z += increment;
				changed = true;
			}
            // prograde decrement
			if(Input.GetKeyDown(progDec)) {
				node.DeltaV.z -= increment;
				node.attachedGizmo.DeltaV.z -= increment;
				changed = true;
			}
            // normal increment
			if(Input.GetKeyDown(normInc)) {
				node.DeltaV.y += increment;
				node.attachedGizmo.DeltaV.y += increment;
				changed = true;
			}
            // normal decrement
			if(Input.GetKeyDown(normDec)) {
				node.DeltaV.y -= increment;
				node.attachedGizmo.DeltaV.y -= increment;
				changed = true;
			}
            // radial increment
			if(Input.GetKeyDown(radiInc)) {
				node.DeltaV.x += increment;
				node.attachedGizmo.DeltaV.x += increment;
				changed = true;
			}
            // radial decrement
			if(Input.GetKeyDown(radiDec)) {
				node.DeltaV.x -= increment;
				node.attachedGizmo.DeltaV.x -= increment;
				changed = true;
			}
            // UT increment
			if(Input.GetKeyDown(timeInc)) {
				node.UT += increment;
				node.attachedGizmo.UT += increment;
				lastUT = node.UT;
				changed = true;
			}
            // UT decrement
			if(Input.GetKeyDown(timeDec)) {
				node.UT -= increment;
				node.attachedGizmo.UT -= increment;
				lastUT = node.UT;
				changed = true;
			}
            // Page Conics
			if(Input.GetKeyDown(pageConics)) {
				if(conicsMode < 0) {
					conicsMode = 0;
				}
				conicsMode++;
				if(conicsMode > 4) {
					conicsMode = 0;
				}
				tools.changeConicsMode(conicsMode);
			}
			// hide/show window
			if(Input.GetKeyDown(hideWindow)) {
				shown = !shown;
			}
			// open node gizmo
			if(Input.GetKeyDown(addWidget)) {
				tools.CreateNodeGizmo(node);
			}
			// If anything changed update the flightplan.
			if(changed) {
				node.solver.UpdateFlightPlan();
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
					mainWindowPos.x = config.GetValue<int>("mainWindowX", Screen.width / 10);
					mainWindowPos.y = config.GetValue<int>("mainWindowY", 20);
					optionsWindowPos.x = config.GetValue<int>("optWindowX", Screen.width / 3);
					optionsWindowPos.y = config.GetValue<int>("optWindowY", 20);
					keymapperWindowPos.x = config.GetValue<int>("keyWindowX", Screen.width / 5);
					keymapperWindowPos.y = config.GetValue<int>("keyWindowY", 20);
					clockWindowPos.x = config.GetValue<int>("clockWindowX", Screen.width / 3);
					clockWindowPos.y = config.GetValue<int>("clockWindowY", Screen.height / 2);
					conicsWindowPos.x = config.GetValue<int>("conicsWindowX", Screen.width / 5);
					conicsWindowPos.y = config.GetValue<int>("conicsWindowY", Screen.height / 2);
					tripWindowPos.x = config.GetValue<int>("tripWindowX", Screen.width / 5);
					tripWindowPos.y = config.GetValue<int>("tripWindowY", Screen.height / 5);
					showClock = config.GetValue<bool>("showClock", false);
					showEAngle = config.GetValue<bool>("showEAngle", true);
					showConicsAlways = config.GetValue<bool>("showConicsAlways", false);
					showOrbitInfo = config.GetValue<bool>("showOrbitInfo", false);
					showUTControls = config.GetValue<bool>("showUTControls", false);
					showManeuverPager = config.GetValue<bool>("showManeuverPager", true);

					string temp = config.GetValue<String>("progInc", "Keypad8");
					progInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("progDec", "Keypad5");
					progDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("normInc", "Keypad9");
					normInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("normDec", "Keypad7");
					normDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("radiInc", "Keypad6");
					radiInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("radiDec", "Keypad4");
					radiDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("timeInc", "Keypad3");
					timeInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("timeDec", "Keypad1");
					timeDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("pageIncrement", "Keypad0");
					pageIncrement = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("pageConics", "KeypadEnter");
					pageConics = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("hideWindow", "P");
					hideWindow = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("addWidget", "O");
					addWidget = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
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
			config["progInc"] = progInc.ToString();
			config["progDec"] = progDec.ToString();
			config["normInc"] = normInc.ToString();
			config["normDec"] = normDec.ToString();
			config["radiInc"] = radiInc.ToString();
			config["radiDec"] = radiDec.ToString();
			config["timeInc"] = timeInc.ToString();
			config["timeDec"] = timeDec.ToString();
			config["pageIncrement"] = pageIncrement.ToString();
			config["pageConics"] = pageConics.ToString();
			config["hideWindow"] = hideWindow.ToString();
			config["addWidget"] = addWidget.ToString();
            config["mainWindowX"] = (int)mainWindowPos.x;
			config["mainWindowY"] = (int)mainWindowPos.y;
			config["optWindowX"] = (int)optionsWindowPos.x;
			config["optWindowY"] = (int)optionsWindowPos.y;
			config["keyWindowX"] = (int)keymapperWindowPos.x;
			config["keyWindowY"] = (int)keymapperWindowPos.y;
			config["clockWindowX"] = (int)clockWindowPos.x;
			config["clockWindowY"] = (int)clockWindowPos.y;
			config["conicsWindowX"] = (int)conicsWindowPos.x;
			config["conicsWindowY"] = (int)conicsWindowPos.y;
			config["tripWindowX"] = (int)tripWindowPos.x;
			config["tripWindowY"] = (int)tripWindowPos.y;
			config["showClock"] = showClock;
			config["showEAngle"] = showEAngle;
			config["showConicsAlways"] = showConicsAlways;
			config["showOrbitInfo"] = showOrbitInfo;
			config["showUTControls"] = showUTControls;
			config["showManeuverPager"] = showManeuverPager;

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

		private enum DISPLAYS {
			SHOWOPTIONS,
			SHOWKEYMAPPER,
			SHOWMANEUVERPAGER,
			SHOWCONICSALWAYS,
			SHOWCLOCK,
			SHOWTRIP,
			SHOWUTCONTROLS,
			SHOWEANGLE,
			SHOWORBITINFO,
			SHOWNEXTENCOUNTER,
			HASNODE
		};
	}	
}
