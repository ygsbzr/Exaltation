using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Modding;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.UI;
using GlobalEnums;
using ModCommon.Util;

namespace Exaltation
{
	public class Exaltation : Mod<SaveSettings>
	{

		private const float BASE_SPEED = 8.3f;
		private const float BASE_SPEED_CH = 10f; //sprintmaster - 20% increase
		private const float BASE_SPEED_CH_COMBO = 11.5f; //sprintmaster + dashmaster = 37% increase
		private const float BASE_SPEED_CH_GLORY = 11.62f; //glorified sprintmaster = ~40% increase
		private const float BASE_SPEED_CH_GLORY_COMBO = 12.1f; //glorified sprintmaster + dashmaster = 45% increase
		private const float BASE_SPEED_CH_GLORYMACHINEWOKE = 12.45f; //glorified sprintmaster + glorified dashmaster = 50% increase

		private const float BASE_ATTACK_DURATION_CH = 0.25f;
		private const float BASE_ATTACK_RECOVERY_CH = 0.25f;
		private const float STEEL_TEMPEST_ATTACK_DURATION = 0.1f;
		private const float STEEL_TEMPEST_ATTACK_COOLDOWN = 0.05f;

		private const float BASE_HIVEBLOOD_SPEED = 5f; //10 seconds
		private const float AMPOULE_HIVEBLOOD_SPEED = 2.5f; //5 seconds

		private float StoneshellRegenTime = 0f;
		private const float STONESHELL_REGEN_WAIT = 10f;

		private const int MONOMON_LENS_SOUL_PER_DAMAGE = 3;
		private const float MONOMON_LENS_MAX_INCREASE = 25;

		private float NailsageRegenTime = 0f;
		private const float NAILSAGE_SOUL_WAIT = 0.75f;
		private const int NAILSAGE_SOUL_REGEN = 4;

		private bool WyrmfuryDeathProtection = true;
		private GameObject CanvasObject;
		private GameObject TextCanvas; //use a different canvas for text since it's handled differently
		private Text TextObject;
		private GameObject WyrmfuryIcon;

		internal static Exaltation Instance;
		public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();
		internal Dictionary<string, Sprite> Sprites;
		internal Dictionary<string, Sprite> CachedSprites;
		private static FieldInfo GeoControlSize = typeof(GeoControl).GetField("size", BindingFlags.NonPublic | BindingFlags.Instance);
		private static MethodInfo ClinkClink = typeof(GeoControl).GetMethod("PlayCollectSound", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo SpriteField = typeof(HeroController).GetField("spriteFlash", BindingFlags.Instance | BindingFlags.NonPublic);

		private int[] CharmNums = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 14, 16, 19, 20, 21, 26, 27, 29, 31, 32, 33, 35, 37 }; //the charm numbers that can be glorified go here for sprites and the like

		public void OnHeroUpdate()
		{
			MakeCanvas();
			GameManager.instance.StartCoroutine(ChangeSprites());
			if (WearingGlorifiedCharm("FuryOfTheFallen"))
				UpdateWyrmfuryIcon();
			if (WearingGlorifiedCharm("BaldurShell") && PlayerData.instance.blockerHits < 4)
			{
				StoneshellRegenTime -= Time.deltaTime;
				if (StoneshellRegenTime <= 0)
				{
					StoneshellRegenTime = STONESHELL_REGEN_WAIT;
					PlayerData.instance.blockerHits++;
					HeroController.instance.GetAttr<AudioSource>("audioSource")
						.PlayOneShot(LoadAssets.BlockerSound, 1f);
					((SpriteFlash)SpriteField.GetValue(HeroController.instance)).flash(Color.blue, 0.5f, 0.0f, 0.0f, 0.5f);
				}
			}
			/*if (Input.GetKeyDown(KeyCode.H)) // Uncomment this for debug purposes.
			{
				const bool glorified = Settings.SprintmasterGlorified;
				Settings.SprintmasterGlorified = glorified;
				Settings.DashmasterGlorified = glorified;
				Settings.StalwartShellGlorified = glorified;
				Settings.GatheringSwarmGlorified = glorified;
				Settings.ShamanStoneGlorified = glorified;
				Settings.GrubsongGlorified = glorified;
				Settings.QuickSlashGlorified = glorified;
				Settings.FuryOfTheFallenGlorified = glorified;
				Settings.HivebloodGlorified = glorified;
				Settings.SoulCatcherGlorified = glorified;
				Settings.QuickFocusGlorified = glorified;
				Settings.SharpShadowGlorified = glorified;
				Settings.LifebloodCoreGlorified = glorified;
				Settings.LifebloodHeartGlorified = glorified;
				Settings.BaldurShellGlorified = glorified;
				Settings.SoulEaterGlorified = glorified;
				Settings.JonisBlessingGlorified = glorified;
				Settings.SpellTwisterGlorified = glorified;
				Settings.WaywardCompassGlorified = glorified;
				Settings.PantheonOneGlory = glorified;
				Settings.PantheonTwoGlory = glorified;
				Settings.PantheonThreeGlory = glorified;
				Settings.PantheonFourGlory = glorified;
				Log("Charm glorification set to " + glorified);
			}
			if (Input.GetKeyDown(KeyCode.J))
			{
				Settings.FotFShade = !Settings.FotFShade;
				Settings.NMGPatience = !Settings.NMGPatience;
				Log("NMG + FotF flavor switched");
			}
			if (Input.GetKeyDown(KeyCode.K))
			{
				HeroController.instance.StartCoroutine(GloryEffects("Glorified by the god of Testing"));
				Log("Glorification effects tested");
			}*/
			if (HeroController.instance.cState.nearBench && (WearingGlorifiedCharm("SoulCatcher") || WearingGlorifiedCharm("SoulEater")))
				HeroController.instance.AddMPChargeSpa(1);
			if (WearingGlorifiedCharm("NailmastersGlory") && Settings.NMGPatience)
			{
				NailsageRegenTime -= Time.deltaTime;
				if (NailsageRegenTime <= 0)
				{
					NailsageRegenTime = NAILSAGE_SOUL_WAIT;
					HeroController.instance.AddMPCharge(GainSoul(NAILSAGE_SOUL_REGEN));
				}
			}
		}

		public string LanguageGet(string key, string sheet)
		{
			if (IsGlorified("GatheringSwarm"))
			{
				if (key == "CHARM_NAME_1")
					return "Symbol of Avarice";
				else if (key == "CHARM_DESC_1")
					return "Prized possession of a powerful bug who fell to their own greed.\n\n" +
						"Geo will be transferred to your hoard instead of dropping onto the ground, ensuring that every last piece is collected and put into its rightful place.";
			}
			if (IsGlorified("WaywardCompass"))
			{
				if (key == "CHARM_NAME_2")
					return "Lifeseed Lantern";
				else if (key == "CHARM_DESC_2")
					return "Glass lantern containing a Lifeseed. It is said that Lifeseeds' antennae will always face northward.\n\n" +
						"The bearer will be able to pinpoint their current location on their map, and gain a very modest lifeblood coating.\n\n" +
						"Requires no charm notches.";
			}
			if (IsGlorified("Grubsong"))
			{
				if (key == "CHARM_NAME_3")
					return "Grubberfly Hymn";
				else if (key == "CHARM_DESC_3")
					return "Contains the tenacity of the grubberfly.\n\n" +
						"Gain SOUL when taking damage, and gain more SOUL when striking foes with the nail based upon missing health.";
			}
			if (IsGlorified("StalwartShell"))
			{
				if(key == "CHARM_NAME_4")
					return "Kingsmould Carapace";
				else if(key == "CHARM_DESC_4")
					return "White metal vessel used to shape and harness void material.\n\n" +
						"The bearer will remain invulnerable for longer when recovering from damage. Additionally, their SOUL will be used to lower the damage of overwhelming strikes against them.";
			}
			if (IsGlorified("BaldurShell"))
			{
				if (key == "CHARM_NAME_5")
					return "Baldur Stoneshell";
				else if (key == "CHARM_DESC_5")
					return "Rocky exoskeleton that protects its bearer with a hard shell while focusing SOUL.\n\n" +
						"The shell is not indestructible, but slowly repairs damage over time.";
			}
			if (IsGlorified("FuryOfTheFallen"))
			{
				if (key == "CHARM_NAME_6")
					return (Settings.FotFShade ? "Shade" : "Wyrm") + "fury";
				else if (key == "CHARM_DESC_6")
					return Settings.FotFShade ?
						"Charm embodying the void's patience and resilience.\n\n" +
						"When close to death, the energy contained within will fill the bearer with stillness and cold focus, and will absorb a single blow that would strike them down." :
						"Charm born of Hallownest's refusal to bend to the old light.\n\n" +
						"When close to death, the energy contained within will fill its bearer with the courage to defy death, and will absorb a single blow that would strike them down.";
			}
			if (IsGlorified("QuickFocus"))
			{
				if (key == "CHARM_NAME_7")
					return "Swift Focus";
				else if (key == "CHARM_DESC_7")
					return "A charm containing crystallized SOUL.\n\n" +
						"Increases the speed and decreases the cost of focusing SOUL.";
			}
			if (IsGlorified("LifebloodHeart"))
			{
				if (key == "CHARM_NAME_8")
					return "Lifeblood Crux";
				else if (key == "CHARM_DESC_8")
					return "Contains a living core that exudes precious lifeblood.\n\n" +
						"When resting, the bearer will gain a coating of lifeblood that protects from a large amount of damage.";
			}
			if (IsGlorified("LifebloodCore"))
			{
				if (key == "CHARM_NAME_9")
					return "Lifeblood Nucleus";
				else if (key == "CHARM_DESC_9")
					return "Contains a living core that flows with precious lifeblood.\n\n" +
						"When resting, the bearer will gain a coating of lifeblood that protects from a great amount of damage.";
			}
			if (IsGlorified("ThornsOfAgony"))
			{
				if (key == "CHARM_NAME_12")
					return "Palace Rose";
				else if (key == "CHARM_DESC_12")
					return "Hardy, colorless rose taken from the White Palace. Bristles with menacing thorns.\n\n" +
						"When taking damage, sprout mystical vines that greatly damage nearby foes.";
			}
			if (IsGlorified("SteadyBody"))
			{
				if (key == "CHARM_NAME_14")
					return "White Sprig";
				else if (key == "CHARM_DESC_14")
					return "A branch of bark, taken from a pale root in the Queen's Gardens.\n\n" +
						"Keeps its bearer from recoiling backwards when they strike an enemy with a nail.\n\n" +
						"Requires no charm notches.";
			}
			if (IsGlorified("SharpShadow"))
			{
				if (key == "CHARM_NAME_16")
					return "Razor Shadow";
				else if (key == "CHARM_DESC_16")
					return "Contains a whispering, eldritch spell that sharpens shadows into weapons.\n\n" +
						"When using Shadow Dash, the bearer's body will cut through enemies like silk, and remains incorporeal for a short time afterwards.";
			}
			if (IsGlorified("ShamanStone"))
			{
				if(key == "CHARM_NAME_19")
					return "Esoteric Egg";
				else if(key == "CHARM_DESC_19")
					return "Mysterious stone egg from before the birth of Hallownest. Its shell has been peeled away, revealing the power contained within.\n\n" +
						"Greatly increases the power of spells, dealing much more damage to foes.";
			}
			if (IsGlorified("SoulCatcher"))
			{
				if (key == "CHARM_NAME_20")
					return "Soul Snare";
				else if (key == "CHARM_DESC_20")
					return "Used to capture large amounts of SOUL from the world around it.\n\n" +
						"Modestly increases the amount of SOUL gained when striking an enemy with the nail, and quickly regenerates SOUL while at a bench.";
			}
			if (IsGlorified("SoulEater"))
			{
				if (key == "CHARM_NAME_21")
					return "Soul Feeder";
				else if (key == "CHARM_DESC_21")
					return "Void liquid contained in a metal vessel, perfectly still. Endlessly consumes SOUL from the world around it.\n\n" +
						"Incomparably increases the amount of SOUL gained when striking an enemy with the nail, and quickly regenerates SOUL while at a bench.";
			}
			if (IsGlorified("NailmastersGlory"))
			{
				if (key == "CHARM_NAME_26")
					return Settings.NMGPatience ? "Sagesoul" : "Nailsage's Tenacity";
				else if (key == "CHARM_DESC_26")
					return Settings.NMGPatience ?
						"Charm ensorcelled with a fraction of the Kingsoul's power.\n\n" +
						"Improves the bearer's mastery of Nail Arts and slowly draws SOUL from the surrounding world.\n\n" +
						"The bearer's nail attacks will gain no SOUL, but will slice easily through armor." :
						"Contains the timeless ferocity and vigor of a Nailsage.\n\n" +
						"Improves the bearer's mastery of Nail Arts and increases the power of their nail strikes as they near death.";
			}
			if (IsGlorified("JonisBlessing"))
			{
				if (key == "CHARM_NAME_27")
					return "Ancestral Blessing";
				else if (key == "CHARM_DESC_27")
					return "Object of worship by the shamans. Transmogrifies vital fluids into blue lifeblood.\n\n" +
						"The bearer will have a healthier shell and can take much more damage, but they will not be able to heal themselves by focusing SOUL.";
			}
			if (IsGlorified("Hiveblood"))
			{
				if (key == "CHARM_NAME_29")
					return "Ambrosial Ampoule";
				else if (key == "CHARM_DESC_29")
					return "Golden nugget of hardened nectar from the Hive that has been compressed into a metal shell from the Crystal Peak.\n\n" +
						"Quickly heals the bearer's recent wounds over time, allowing them to regain some health without focusing SOUL.";
			}
			if (IsGlorified("Dashmaster"))	
			{
				if(key == "CHARM_NAME_31")
					return "Racemaster";
				else if(key == "CHARM_DESC_31")
					return "Bears the likeness of an eccentric bug known only as ‘The Dashmaster', in true form.\n\n" +
						"The bearer will be able to dash more often as well as dash downwards.";
			}
			if (IsGlorified("QuickSlash"))
			{
				if (key == "CHARM_NAME_32")
					return "Steel Tempest";
				else if (key == "CHARM_DESC_32")
					return "A bladed disc forged from the metal of imperfect nails. Emits a whistling sound when thrust through the air.\n\n" +
						"The bearer's nail will become like a storm of metal, moderately decreasing its damage but tremendously increasing its swinging speed.";
			}
			if (IsGlorified("SpellTwister"))
			{
				if (key == "CHARM_NAME_33")
					return "Prismatic Lens";
				else if (key == "CHARM_DESC_33")
					return "Lens molded from fog that shifts and glimmers in the light.\n\n" +
						"Reduces the SOUL cost of casting spells, and empowers them based on remaining SOUL after the spell is cast.";
			}
			if (IsGlorified("Sprintmaster"))
			{
				if(key == "CHARM_NAME_37")
					return "Stagway Coin";
				else if(key == "CHARM_DESC_37")
					return "A tarnished symbol once held by the upper caste of Hallownest. Each coin allowed priority access to the Stagways, should its holder prefer the old paths.\n\n" +
						"Greatly increases the running speed of its bearer, allowing them to expedite their travels.";
			}
			if (key == "CHARM_DESC_36_B")
				return "Holy charm symbolising a union between higher beings. The bearer will slowly absorb the limitless SOUL contained within.\n\n" +
					"Opens the way to a birthplace.\n\n" +
					"First, however, be certain one has no more use for the power inside.";
			return Language.Language.GetInternal(key, sheet);
		}

		private void BeforeSaveGameSave(SaveGameData data = null)
		{
			PlayerData.instance.charmCost_2 = 1;
			PlayerData.instance.charmCost_14 = 1;
			PlayerData.instance.charmCost_29 = 3;
			PlayerData.instance.charmCost_31 = 2;
		}

		private void SaveGameSave(int id = 0)
		{
			if (IsGlorified("WaywardCompass"))
				PlayerData.instance.charmCost_2 = 0;
			if (IsGlorified("SteadyBody"))
				PlayerData.instance.charmCost_14 = 0;
			if (IsGlorified("Hiveblood"))
				PlayerData.instance.charmCost_29 = 3;
			if (IsGlorified("Dashmaster"))
				PlayerData.instance.charmCost_31 = 1;
		}

		private void AfterSaveGameLoad(SaveGameData data)
		{
			GameManager.instance.StartCoroutine(ChangeSprites());
		}
		private void ProcessGeoUpdate(On.GeoControl.orig_OnEnable orig, GeoControl self)
		{
			orig(self);
			if (WearingGlorifiedCharm("GatheringSwarm")) //with symbol of avarice, instantly transfer geo rather than drop it
			{
				GeoControl.Size size = (GeoControl.Size)GeoControlSize.GetValue(self);
				HeroController.instance.AddGeo(size.value); //get a reflection of however much geo there is total, so that we don't lose any
				ClinkClink.Invoke(self, null);
				self.Disable(0.05f);
			}
		}

		private void OnCharmUpdate(PlayerData pd, HeroController hc)
		{
			if (TextObject == null)
				MakeCanvas();
			else
			{
				bool glorified_this_update = false;
				if (hc != null)
				{
					if (PlayerData.instance.killedNailBros && !PantheonGlorified(1) && !glorified_this_update)
					{
						Settings.GatheringSwarmGlorified = true;
						Settings.WaywardCompassGlorified = true;
						Settings.GrubsongGlorified = true;
						Settings.StalwartShellGlorified = true;
						Settings.BaldurShellGlorified = true;
						Settings.SteadyBodyGlorified = true;
						glorified_this_update = true;
						hc.StartCoroutine(GloryEffects("Charms glorified by the gods of brotherhood"));
					}
					if (PlayerData.instance.killedPaintmaster && !PantheonGlorified(2) && !glorified_this_update)
					{
						Settings.LifebloodHeartGlorified = true;
						Settings.LifebloodCoreGlorified = true;
						Settings.JonisBlessingGlorified = true;
						Settings.FuryOfTheFallenGlorified = true;
						Settings.ThornsOfAgonyGlorified = true;
						Settings.NailmastersGloryGlorified = true;
						Settings.FotFShade = pd.gotShadeCharm ? true : false;
						Settings.NMGPatience = pd.gotShadeCharm ? false : true;
						glorified_this_update = true;
						hc.StartCoroutine(GloryEffects("Charms glorified by the god of creation"));
					}
					if (PlayerData.instance.killedNailsage && !PantheonGlorified(3) && !glorified_this_update)
					{
						Settings.SoulCatcherGlorified = true;
						Settings.SoulEaterGlorified = true;
						Settings.DashmasterGlorified = true;
						Settings.SprintmasterGlorified = true;
						Settings.SharpShadowGlorified = true;
						glorified_this_update = true;
						hc.StartCoroutine(GloryEffects("Charms glorified by the god of opportunity"));
					}
					if (PlayerData.instance.killedHollowKnightPrime && !PantheonGlorified(4) && !glorified_this_update)
					{
						Settings.ShamanStoneGlorified = true;
						Settings.SpellTwisterGlorified = true;
						Settings.QuickSlashGlorified = true;
						Settings.QuickFocusGlorified = true;
						Settings.HivebloodGlorified = true;
						glorified_this_update = true;
						hc.StartCoroutine(GloryEffects("Charms glorified by the god of nothingness"));
					}
				}
			}
			UpdateMoveSpeed();
			WyrmfuryDeathProtection = true; //reset death protection when resting
			if(WearingGlorifiedCharm("QuickSlash"))
			{
				hc.ATTACK_COOLDOWN_TIME_CH = STEEL_TEMPEST_ATTACK_COOLDOWN; //nyoooommmm
				hc.ATTACK_DURATION_CH = STEEL_TEMPEST_ATTACK_DURATION;
			}
			else
			{
				hc.ATTACK_COOLDOWN_TIME_CH = BASE_ATTACK_RECOVERY_CH;
				hc.ATTACK_DURATION_CH = BASE_ATTACK_DURATION_CH;
			}
			pd.focusMP_amount = WearingGlorifiedCharm("QuickFocus") ? 24 : 33;
			GameObject helf = GameObject.Find("Health");
			if (helf != null)
			{
				helf.LocateMyFSM("Hive Health Regen").
					Fsm.GetFsmFloat("Recover Time").
					Value = WearingGlorifiedCharm("Hiveblood") ? AMPOULE_HIVEBLOOD_SPEED : BASE_HIVEBLOOD_SPEED;
			}
			pd.charmCost_2 = IsGlorified("WaywardCompass") ? 0 : 1;
			pd.charmCost_14 = IsGlorified("SteadyBody") ? 0 : 1;
			pd.charmCost_29 = IsGlorified("Hiveblood") ? 3 : 4;
			pd.charmCost_31 = IsGlorified("Dashmaster") ? 1 : 2;
		}

		private int TakeDamage(int amount)
		{
			PlayerData pd = PlayerData.instance;
			if(pd.maxHealth <= amount) //only protect from damage if we aren't at max health; mainly for radiant bosses
				return amount;
			if(amount >= 2 && pd.MPCharge >= pd.focusMP_amount && WearingGlorifiedCharm("StalwartShell"))
			{
				amount--; // reduces high damage by 1 mask!
				HeroController.instance.TakeMP(pd.focusMP_amount);
				HeroController.instance.GetAttr<AudioSource>("audioSource")
					.PlayOneShot(LoadAssets.ShellSound, 1f);
			}
			if(pd.health <= amount && WyrmfuryDeathProtection && WearingGlorifiedCharm("FuryOfTheFallen"))
			{
				amount = pd.health - 1; //brings to 1 HP if you're not there already
				WyrmfuryDeathProtection = false; //nullify the hit!
				HeroController.instance.GetAttr<AudioSource>("audioSource")
					.PlayOneShot(LoadAssets.WyrmfurySound, 1f);
				GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
			}
			StoneshellRegenTime = STONESHELL_REGEN_WAIT; //prevent the hit from regenerating
			return amount;
		}

		private int LifebloodMasksRestored()
		{
			int masks = 0;
			if (WearingGlorifiedCharm("WaywardCompass"))
				masks++; //lifeseed!
			if (WearingGlorifiedCharm("LifebloodHeart"))
				masks += 2;
			if (WearingGlorifiedCharm("LifebloodCore"))
				masks += 2;
			if (WearingGlorifiedCharm("JonisBlessing"))
				masks += 4; //BIG MASKS
			return masks;
		}

		private void UpdateMoveSpeed(float mod = 1f)
		{
			if (!WearingGlorifiedCharm("Sprintmaster"))
			{
				HeroController.instance.RUN_SPEED = BASE_SPEED * mod;
				HeroController.instance.RUN_SPEED_CH = BASE_SPEED_CH * mod;
				HeroController.instance.RUN_SPEED_CH_COMBO = BASE_SPEED_CH_COMBO * mod;
			}
			else
			{
				HeroController.instance.RUN_SPEED_CH = BASE_SPEED_CH_GLORY * mod;
				HeroController.instance.RUN_SPEED_CH_COMBO = BASE_SPEED_CH_GLORY_COMBO * mod;
				if (WearingGlorifiedCharm("Dashmaster"))
					HeroController.instance.RUN_SPEED_CH_COMBO = BASE_SPEED_CH_GLORYMACHINEWOKE * mod;
			}
		}

		private int GainSoul(int amount)
		{
			if (WearingGlorifiedCharm("Grubsong"))
			{
				int MissingHealth = PlayerData.instance.maxHealth - PlayerData.instance.health;
				amount += 1 * MissingHealth;
			}
			if (WearingGlorifiedCharm("SoulCatcher"))
				amount += 2; //Vanilla soul catcher is +3, so +2 = +5%
			if (WearingGlorifiedCharm("SoulEater"))
				amount += 3; //Vanilla soul eater is +8, so +3 = +11% - double the base!
			if (WearingGlorifiedCharm("NailmastersGlory") && Settings.NMGPatience && amount >= NAILSAGE_SOUL_REGEN + 1)
				amount = NAILSAGE_SOUL_REGEN + 1; //allow synergization but don't make it overpowered
			return amount;
		}

		private HitInstance HitInstanceAdjust(Fsm owner, HitInstance hit)
		{
			string ParentName = hit.Source.transform.parent.name; //note - for many attacks this will be null; be careful
			if (ParentName != null && ParentName == "Thorn Hit" && IsGlorified("ThornsOfAgony"))
			{
				hit.DamageDealt = (int)(hit.DamageDealt * 1.5);
				hit.AttackType = AttackTypes.Spell; //palace rose thorns are spell-type instead of normal-type
			}
			if (hit.AttackType == AttackTypes.Spell)
			{
				if (WearingGlorifiedCharm("ShamanStone"))
					hit.DamageDealt = (int)(hit.DamageDealt * 1.125f);
				if (WearingGlorifiedCharm("SpellTwister"))
				{
					float DamageIncrease = PlayerData.instance.MPCharge / MONOMON_LENS_SOUL_PER_DAMAGE;
					if (DamageIncrease > MONOMON_LENS_MAX_INCREASE)
						DamageIncrease = MONOMON_LENS_MAX_INCREASE;
					DamageIncrease /= 100; //turn "25", "10" etc. into 0.25f, 0.1f
					DamageIncrease += 1f; //turn 0.25f, 0.1f etc. into 1.25f, 1.1f
					hit.DamageDealt = (int)(hit.DamageDealt * DamageIncrease);
				}
			}
			if (hit.AttackType == AttackTypes.Nail)
			{
				if (WearingGlorifiedCharm("QuickSlash"))
					hit.DamageDealt = (int)(hit.DamageDealt * 0.75f);
				if (WearingGlorifiedCharm("FuryOfTheFallen") && PlayerData.instance.health == 1)
					hit.DamageDealt = (int)(hit.DamageDealt * 1.15f);
				if (WearingGlorifiedCharm("NailmastersGlory"))
					if (Settings.NMGPatience) //change this AFTER modifying spell damage to avoid massive damage stacking
						hit.AttackType = AttackTypes.Spell;
					else
						hit.DamageDealt += (int)(hit.DamageDealt * 0.03f * (PlayerData.instance.maxHealth - PlayerData.instance.health));
			}
			if (hit.AttackType == AttackTypes.SharpShadow && WearingGlorifiedCharm("SharpShadow"))
			{
				hit.DamageDealt *= 2;
			}
			return hit;
		}

		private bool DashPressed()
		{
			IEnumerator RazorShadow()
			{
				while (HeroController.instance.cState.shadowDashing)
					yield return null;
				PlayerData.instance.isInvincible = true;
				((SpriteFlash)SpriteField.GetValue(HeroController.instance)).flash(Color.black, 1.11f, 0.1f, 0.8f, 0.2f);
				yield return new WaitForSeconds(0.8f);
				PlayerData.instance.isInvincible = false;
			}
			if(WearingGlorifiedCharm("SharpShadow"))
				HeroController.instance.StartCoroutine(RazorShadow());
			return false;
		}

		private bool InInventory()
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("Inventory Top");
			if (gameObject == null)
				return false;
			PlayMakerFSM component = FSMUtility.LocateFSM(gameObject, "Inventory Control");
			if (component == null)
				return false;
			FsmBool fsmBool = component.FsmVariables.GetFsmBool("Open");
			return fsmBool != null && fsmBool.Value;
		}

		private IEnumerator ChangeSprites()
		{
			while (CharmIconList.Instance == null || GameManager.instance == null || HeroController.instance == null)
				yield return null;
			if(CachedSprites.Count == 0)
			{
				foreach ( int i in CharmNums ) //num num =^.^=
				{
					CachedSprites.Add(i.ToString(), CharmIconList.Instance.spriteList[i]);
					Log("Cached vanilla sprite for charm number " + i);
				}
				Log("Charm sprite list length: " + CharmIconList.Instance.spriteList.Length);
				/*for (int i = 0; i <= CharmIconList.Instance.spriteList.Length; i++)
					Log("Charm " + i + " sprite: " + CharmIconList.Instance.spriteList[i].name);*/
			}
			foreach ( int i in CharmNums) //okay I want to die after writing that first comment
				CharmIconList.Instance.spriteList[i] = IsGlorified(i.ToString()) ? Sprites["Exaltation.Resources.Charms." + i + ".png"] : CachedSprites[i.ToString()];
			if (IsGlorified("FuryOfTheFallen") && Settings.FotFShade) //FotF has unique variants
				CharmIconList.Instance.spriteList[6] = Sprites["Exaltation.Resources.Charms.6_shade.png"];
			if (IsGlorified("NailmastersGlory") && Settings.NMGPatience) //and NMG is different entirely if made with the kingsoul
				CharmIconList.Instance.spriteList[26] = Sprites["Exaltation.Resources.Charms.26_patience.png"];
		}

		private void MakeCanvas()
		{
			if (CanvasObject == null)
				CanvasObject = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920f, 1080f));

			if (WyrmfuryIcon == null && HeroController.instance != null && WearingGlorifiedCharm("FuryOfTheFallen"))
			{
				WyrmfuryIcon = CanvasUtil.CreateImagePanel(CanvasObject,
					Sprites["Exaltation.Resources.WyrmfuryIcon.png"],
					new CanvasUtil.RectData(new Vector2(50f, 50f), new Vector2(0.13f, 0.78f),
					new Vector2(0.13f, 0.78f), new Vector2(0.13f, 0.78f)));

				Image WyrmfuryPicture = WyrmfuryIcon.GetComponent<Image>();

				WyrmfuryPicture.preserveAspect = false;
				WyrmfuryPicture.type = Image.Type.Filled;
				WyrmfuryPicture.fillMethod = Image.FillMethod.Horizontal;
				WyrmfuryPicture.fillAmount = 1f;
			}

			if (TextCanvas == null)
			{
				CanvasUtil.CreateFonts();
				TextCanvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920f, 1080f));
				GameObject TextPanel = CanvasUtil.CreateTextPanel(TextCanvas, "", 27, TextAnchor.MiddleCenter,
					new CanvasUtil.RectData(
						new Vector2(0, 50),
						new Vector2(0, 45),
						new Vector2(0, 0),
						new Vector2(1, 0),
						new Vector2(0.5f, 0.5f)));
				TextObject = TextPanel.GetComponent<Text>();
				TextObject.font = CanvasUtil.TrajanBold;
				TextObject.text = "";
				TextObject.fontSize = 42;
			}
		}

		private void UpdateWyrmfuryIcon()
		{
			if(WyrmfuryIcon == null)
				return;
			Image WyrmfuryPicture = WyrmfuryIcon.GetComponent<Image>();
			if (!WearingGlorifiedCharm("FuryOfTheFallen") || GameManager.instance == null || GameManager.instance.gameState != GameState.PLAYING || InInventory())
			{
				GameManager.instance.StartCoroutine(CanvasUtil.FadeOutCanvasGroup(CanvasObject.GetComponent<CanvasGroup>()));
				return;
			}
			if(CanvasObject.GetComponent<CanvasGroup>().gameObject.activeSelf == false)
			{
				GameManager.instance.StartCoroutine(CanvasUtil.FadeInCanvasGroup(CanvasObject.GetComponent<CanvasGroup>()));
				WyrmfuryPicture.fillAmount = 1f;
			}
			string Wyrm = !Settings.FotFShade ? "Wyrm" : "Shade";
			string Broken = WyrmfuryDeathProtection ? "Icon" : "Broken";
			WyrmfuryPicture.sprite = Sprites["Exaltation.Resources." + Wyrm + "fury" + Broken + ".png"];
		}

		private bool IsGlorified(string CharmName) //quick function for checking if a charm is glorified
		{
			CharmName = CharmName.ToLower(); //prevent case entry from changing it up
			switch (CharmName)
			{
				case "gatheringswarm":
				case "1":
					return Settings.GatheringSwarmGlorified;
				case "waywardcompass":
				case "2":
					return Settings.WaywardCompassGlorified;
				case "grubsong":
				case "3":
					return Settings.GrubsongGlorified;
				case "stalwartshell":
				case "4":
					return Settings.StalwartShellGlorified;
				case "baldurshell":
				case "5":
					return Settings.BaldurShellGlorified;
				case "furyofthefallen":
				case "6":
					return Settings.FuryOfTheFallenGlorified;
				case "quickfocus":
				case "7":
					return Settings.QuickFocusGlorified;
				case "lifebloodheart":
				case "8":
					return Settings.LifebloodHeartGlorified;
				case "lifebloodcore":
				case "9":
					return Settings.LifebloodCoreGlorified;
				case "thornsofagony":
				case "12":
					return Settings.ThornsOfAgonyGlorified;
				case "steadybody":
				case "14":
					return Settings.SteadyBodyGlorified;
				case "sharpshadow":
				case "16":
					return Settings.SharpShadowGlorified;
				case "shamanstone":
				case "19":
					return Settings.ShamanStoneGlorified;
				case "soulcatcher":
				case "20":
					return Settings.SoulCatcherGlorified;
				case "souleater":
				case "21":
					return Settings.SoulEaterGlorified;
				case "nailmastersglory":
				case "26":
					return Settings.NailmastersGloryGlorified;
				case "jonisblessing":
				case "27":
					return Settings.JonisBlessingGlorified;
				case "hiveblood":
				case "29":
					return Settings.HivebloodGlorified;
				case "dashmaster":
				case "31":
					return Settings.DashmasterGlorified;
				case "quickslash":
				case "32":
					return Settings.QuickSlashGlorified;
				case "spelltwister":
				case "33":
					return Settings.SpellTwisterGlorified;
				case "sprintmaster":
				case "37":
					return Settings.SprintmasterGlorified;
			}
			return false;
		}

		private bool WearingGlorifiedCharm(string CharmName) //sister function to IsGlorified to check if the player is wearing it
		{
			CharmName = CharmName.ToLower();
			switch(CharmName)
			{
				case "gatheringswarm":
					return Settings.GatheringSwarmGlorified && PlayerData.instance.equippedCharm_1;
				case "waywardcompass":
					return Settings.WaywardCompassGlorified && PlayerData.instance.equippedCharm_2;
				case "grubsong":
					return Settings.GrubsongGlorified && PlayerData.instance.equippedCharm_3;
				case "stalwartshell":
					return Settings.StalwartShellGlorified && PlayerData.instance.equippedCharm_4;
				case "baldurshell":
					return Settings.BaldurShellGlorified && PlayerData.instance.equippedCharm_5;
				case "furyofthefallen":
					return Settings.FuryOfTheFallenGlorified && PlayerData.instance.equippedCharm_6;
				case "quickfocus":
					return Settings.QuickFocusGlorified && PlayerData.instance.equippedCharm_7;
				case "lifebloodheart":
					return Settings.LifebloodHeartGlorified && PlayerData.instance.equippedCharm_8;
				case "lifebloodcore":
					return Settings.LifebloodCoreGlorified && PlayerData.instance.equippedCharm_9;
				case "thornsofagony":
					return Settings.ThornsOfAgonyGlorified && PlayerData.instance.equippedCharm_12;
				case "steadybody":
					return Settings.SteadyBodyGlorified && PlayerData.instance.equippedCharm_14;
				case "sharpshadow":
					return Settings.SharpShadowGlorified && PlayerData.instance.equippedCharm_16;
				case "shamanstone":
					return Settings.ShamanStoneGlorified && PlayerData.instance.equippedCharm_19;
				case "soulcatcher":
					return Settings.SoulCatcherGlorified && PlayerData.instance.equippedCharm_20;
				case "souleater":
					return Settings.SoulEaterGlorified && PlayerData.instance.equippedCharm_21;
				case "nailmastersglory":
					return Settings.NailmastersGloryGlorified && PlayerData.instance.equippedCharm_26;
				case "jonisblessing":
					return Settings.JonisBlessingGlorified && PlayerData.instance.equippedCharm_27;
				case "hiveblood":
					return Settings.HivebloodGlorified && PlayerData.instance.equippedCharm_29;
				case "dashmaster":
					return Settings.DashmasterGlorified && PlayerData.instance.equippedCharm_31;
				case "quickslash":
					return Settings.QuickSlashGlorified && PlayerData.instance.equippedCharm_32;
				case "spelltwister":
					return Settings.SpellTwisterGlorified && PlayerData.instance.equippedCharm_33;
				case "sprintmaster":
					return Settings.SprintmasterGlorified && PlayerData.instance.equippedCharm_37;
			}
			return false;
		}

		private bool PantheonGlorified(int pantheon) //this double-checks that everything is set so that past saves are compatible with new updates
		{
			if (pantheon == 1) //tried a switch here, it broke everything
				return Settings.GatheringSwarmGlorified &&
					Settings.WaywardCompassGlorified &&
					Settings.GrubsongGlorified &&
					Settings.StalwartShellGlorified &&
					Settings.BaldurShellGlorified &&
					Settings.SteadyBodyGlorified;
			else if (pantheon == 2)
				return Settings.LifebloodHeartGlorified &&
					Settings.LifebloodCoreGlorified &&
					Settings.JonisBlessingGlorified &&
					Settings.FuryOfTheFallenGlorified &&
					Settings.ThornsOfAgonyGlorified &&
					Settings.NailmastersGloryGlorified;
			else if (pantheon == 3)
				return Settings.SoulCatcherGlorified &&
					Settings.SoulEaterGlorified &&
					Settings.DashmasterGlorified &&
					Settings.SprintmasterGlorified &&
					Settings.SharpShadowGlorified;
			else if (pantheon == 4)
				return Settings.ShamanStoneGlorified &&
					Settings.SpellTwisterGlorified &&
					Settings.QuickSlashGlorified &&
					Settings.QuickFocusGlorified &&
					Settings.HivebloodGlorified;
			return false;
		}

		public override void Initialize()
		{
			Instance = this;
			Log("Exaltation initializing...");

			try { RegisterCallbacks(); }
			catch { Log("Exaltation failed to register callbacks!"); }
			try { LoadAssets.LoadSounds(); }
			catch { Log("Exaltation failed to find glorify sound!"); }
			Log("Sounds loaded.");

			Log("Exaltation initialized.");
		}

		private IEnumerator GloryEffects(string glorytext)
		{
			if (Random.Range(1, 101) == 100)
				glorytext = "Charms glorified by the gods of discord";
			yield return new WaitForSeconds(0.35f);
			TextObject.text = glorytext;
			TextObject.CrossFadeAlpha(1f, 0f, false);
			((SpriteFlash)SpriteField.GetValue(HeroController.instance)).flash(Color.white, 1.75f, 0.25f, 1f, 0.5f);
			HeroController.instance.GetAttr<AudioSource>("audioSource")
				.PlayOneShot(LoadAssets.GlorifySound, 1f);
			GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
			yield return new WaitForSeconds(1.5f);
			TextObject.CrossFadeAlpha(0f, 1f, false);
		}

		private void RegisterCallbacks()
		{
			ModHooks.Instance.HeroUpdateHook += OnHeroUpdate;
			ModHooks.Instance.LanguageGetHook += LanguageGet;

			ModHooks.Instance.BeforeAddHealthHook += TakeDamage;
			ModHooks.Instance.TakeHealthHook += TakeDamage;
			ModHooks.Instance.BlueHealthHook += LifebloodMasksRestored;

			ModHooks.Instance.CharmUpdateHook += OnCharmUpdate;

			ModHooks.Instance.SoulGainHook += GainSoul;

			ModHooks.Instance.HitInstanceHook += HitInstanceAdjust;

			ModHooks.Instance.DashPressedHook += DashPressed;

			ModHooks.Instance.BeforeSavegameSaveHook += BeforeSaveGameSave;
			ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
			ModHooks.Instance.SavegameSaveHook += SaveGameSave;

			Assembly asm = Assembly.GetExecutingAssembly();
			Sprites = new Dictionary<string, Sprite>();
			CachedSprites = new Dictionary<string, Sprite>();
			foreach (string res in asm.GetManifestResourceNames())
			{
				if (!res.EndsWith(".png"))
					continue;

				using (Stream s = asm.GetManifestResourceStream(res))
				{
					if (s == null) continue;
					byte[] buffer = new byte[s.Length];
					s.Read(buffer, 0, buffer.Length);
					s.Dispose();

					//Create texture from bytes
					Texture2D tex = new Texture2D(1, 1);
					tex.LoadImage(buffer);

					//Create sprite from texture
					Sprites.Add(res, Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));

					Log("Created sprite from embedded image: " + res);
				}
			}

			On.GeoControl.OnEnable += ProcessGeoUpdate;
		}
	}
}
