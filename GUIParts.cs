using System;
using UnityEngine;
using KSP.IO;

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
