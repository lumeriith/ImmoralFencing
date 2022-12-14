package mrl.motion.critical.run;

import mrl.motion.neural.agility.match.TransitionDPGenerator;

public class DP_Preprocessing {

	public static void main(String[] args) {
//		String[] actions = MartialArtsConfig.actionTypes;
//		int cLabelSize = MartialArtsConfig.LOCO_ACTION_SIZE;
//		
//		String dataFolder = "martial_arts_compact";
//		String tPoseFile = "data\\t_pose_ue2.bvh";
		// locomotion action size(cyclic actions)
//		TransitionDPGenerator.printDistribution = false;
		
//		String[] actions = DuelConfig.actionTypes;
//		int cLabelSize = DuelConfig.LOCO_ACTION_SIZE;
//		
//		String dataFolder = "duel_0";
//		String tPoseFile = "data\\stop_fencing.bvh";
		
		String[] actions = WalkConfig.actionTypes;
		int cLabelSize = WalkConfig.LOCO_ACTION_SIZE;
		
		String dataFolder = "walk";
		String tPoseFile = "data\\stop_fencing.bvh";
		
//		String[] actions = FencingConfig.actionTypes;
//		int cLabelSize = FencingConfig.LOCO_ACTION_SIZE;
//		
//		String dataFolder = "fencing";
//		String tPoseFile = "data\\stop_fencing.bvh";
		
		TransitionDPGenerator.make(dataFolder, tPoseFile, actions, cLabelSize);
	}
}
