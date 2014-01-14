using System;
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
	public class GUIParts {
		public static void drawDoubleLabel(String text1, float width1, String text2, float width2) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(text1, GUILayout.Width(width1));
			GUILayout.Label(text2, GUILayout.Width(width2));
			GUILayout.EndHorizontal();
		}

		public static void drawButton(String text, Color bgColor, Action callback) {
			Color defaultColor = GUI.backgroundColor;
			GUI.backgroundColor = bgColor;
			if(GUILayout.Button(text)) {
				callback();
			}
			GUI.backgroundColor = defaultColor;
		}

		public static void drawManeuverPager(NodeManager curState) {
			PatchedConicSolver solver = NodeTools.getSolver();

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
	}
}
