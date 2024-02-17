using Fusion;
using FusionHelpers;

namespace FusionGame.Stickman
{
	public static class NetworkRunnerStaticRefs
	{
		public static LevelManager GetLevelManager(this NetworkRunner runner) => runner ? (LevelManager)runner.SceneManager : null;
        public static FusionGame.Stickman.GameManager GetGameManager(this NetworkRunner runner) => runner ? (runner.TryGetSingleton(out FusionSession session) ? (FusionGame.Stickman.GameManager)session: null) : null;
    }
}