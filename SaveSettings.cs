using Modding;

namespace Exaltation
{
	public class SaveSettings : IModSettings
	{
		public bool GatheringSwarmGlorified { get => GetBool(); set => SetBool(value); } //1
		public bool WaywardCompassGlorified { get => GetBool(); set => SetBool(value); } //2
		public bool GrubsongGlorified { get => GetBool(); set => SetBool(value); } //3
		public bool StalwartShellGlorified { get => GetBool(); set => SetBool(value); } //4
		public bool BaldurShellGlorified { get => GetBool(); set => SetBool(value); } //5
		public bool FuryOfTheFallenGlorified { get => GetBool(); set => SetBool(value); } //6
		public bool QuickFocusGlorified { get => GetBool(); set => SetBool(value); } //7
		public bool LifebloodHeartGlorified { get => GetBool(); set => SetBool(value); } //8
		public bool LifebloodCoreGlorified { get => GetBool(); set => SetBool(value); } //8
		public bool ThornsOfAgonyGlorified { get => GetBool(); set => SetBool(value); } //12
		public bool SteadyBodyGlorified { get => GetBool(); set => SetBool(value); } //14
		public bool SharpShadowGlorified { get => GetBool(); set => SetBool(value); } //16
		public bool ShamanStoneGlorified { get => GetBool(); set => SetBool(value); } //19
		public bool SoulCatcherGlorified { get => GetBool(); set => SetBool(value); } //20
		public bool SoulEaterGlorified { get => GetBool(); set => SetBool(value); } //21
		public bool NailmastersGloryGlorified { get => GetBool(); set => SetBool(value); } //26... and a little redundant
		public bool JonisBlessingGlorified { get => GetBool(); set => SetBool(value); } //27
		public bool HivebloodGlorified { get => GetBool(); set => SetBool(value); } //29
		public bool DashmasterGlorified { get => GetBool(); set => SetBool(value); } //31
		public bool QuickSlashGlorified { get => GetBool(); set => SetBool(value); } //32
		public bool SpellTwisterGlorified { get => GetBool(); set => SetBool(value); } //33
		public bool SprintmasterGlorified { get => GetBool(); set => SetBool(value); } //37

		public bool FotFShade { get => GetBool(); set => SetBool(value); }
		public bool NMGPatience { get => GetBool(); set => SetBool(value); }
	}
}
