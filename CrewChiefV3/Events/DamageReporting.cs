using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class DamageReporting : AbstractEvent
    {
        private String folderMinorTransmissionDamage = "damage_reporting/minor_transmission_damage";
        private String folderMinorEngineDamage = "damage_reporting/minor_engine_damage";
        private String folderMinorAeroDamage = "damage_reporting/minor_aero_damage";
        private String folderMinorSuspensionDamage = "damage_reporting/minor_suspension_damage";
        private String folderMinorBrakeDamage = "damage_reporting/minor_brake_damage";

        private String folderSevereTransmissionDamage = "damage_reporting/severe_transmission_damage";
        private String folderSevereEngineDamage = "damage_reporting/severe_engine_damage";
        private String folderSevereAeroDamage = "damage_reporting/severe_aero_damage";
        private String folderSevereBrakeDamage = "damage_reporting/severe_brake_damage";
        private String folderSevereSuspensionDamage = "damage_reporting/severe_suspension_damage";

        private String folderBustedTransmission = "damage_reporting/busted_transmission";
        private String folderBustedEngine = "damage_reporting/busted_engine";
        private String folderBustedSuspension = "damage_reporting/busted_suspension";
        private String folderBustedBrakes = "damage_reporting/busted_brakes";

        private String folderNoTransmissionDamage = "damage_reporting/no_transmission_damage";
        private String folderNoEngineDamage = "damage_reporting/no_engine_damage";
        private String folderNoAeroDamage = "damage_reporting/no_aero_damage"; 
        private String folderNoSuspensionDamage = "damage_reporting/no_suspension_damage"; 
        private String folderNoBrakeDamage = "damage_reporting/no_brake_damage";
        private String folderJustAScratch = "damage_reporting/trivial_aero_damage";

        private String folderMissingWheel = "damage_reporting/missing_wheel";

        private DamageLevel engineDamage;
        private DamageLevel trannyDamage;
        private DamageLevel aeroDamage;
        private DamageLevel maxSuspensionDamage;
        private DamageLevel maxBrakeDamage;

        private Component worstComponent = Component.NONE;

        private DamageLevel worstDamageLevel = DamageLevel.NONE;

        private Boolean isMissingWheel = false;

        private Component lastReportedComponent = Component.NONE;

        private DamageLevel lastReportedDamageLevel = DamageLevel.NONE;

        private TimeSpan timeToWaitForDamageToSettle = TimeSpan.FromSeconds(3);

        private DateTime timeWhenDamageLastChanged = DateTime.MinValue;

        private enum Component
        {
            ENGINE, TRANNY, AERO, SUSPENSION, BRAKES, NONE
        }
        
        public DamageReporting(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            engineDamage = DamageLevel.NONE;
            trannyDamage = DamageLevel.NONE;
            aeroDamage = DamageLevel.NONE;
            maxSuspensionDamage = DamageLevel.NONE;
            maxBrakeDamage = DamageLevel.NONE;
            worstComponent = Component.NONE;
            worstDamageLevel = DamageLevel.NONE;
            lastReportedComponent = Component.NONE;
            lastReportedDamageLevel = DamageLevel.NONE;
            timeWhenDamageLastChanged = DateTime.MinValue;
            isMissingWheel = false;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.CarDamageData.DamageEnabled)
            {
                aeroDamage = currentGameState.CarDamageData.OverallAeroDamage;
                trannyDamage = currentGameState.CarDamageData.OverallTransmissionDamage;
                engineDamage = currentGameState.CarDamageData.OverallEngineDamage;
                if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.DESTROYED))
                {
                    maxBrakeDamage = DamageLevel.DESTROYED;
                }
                else if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.MAJOR))
                {
                    maxBrakeDamage = DamageLevel.MAJOR;
                }
                else if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.MINOR))
                {
                    maxBrakeDamage = DamageLevel.MINOR;
                }
                else if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.TRIVIAL))
                {
                    maxBrakeDamage = DamageLevel.TRIVIAL;
                }

                if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.DESTROYED))
                {
                    maxSuspensionDamage = DamageLevel.DESTROYED;
                }
                else if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.MAJOR))
                {
                    maxSuspensionDamage = DamageLevel.MAJOR;
                }
                else if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.MINOR))
                {
                    maxSuspensionDamage = DamageLevel.MINOR;
                }
                else if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.TRIVIAL))
                {
                    maxSuspensionDamage = DamageLevel.TRIVIAL;
                }
                isMissingWheel = !currentGameState.PitData.InPitlane && (!currentGameState.TyreData.LeftFrontAttached || !currentGameState.TyreData.RightFrontAttached ||
                        !currentGameState.TyreData.LeftRearAttached || !currentGameState.TyreData.RightRearAttached);

                Tuple<Component, DamageLevel> worstDamage = getWorstDamage();
                if (worstDamage.Item1 != lastReportedComponent || worstDamage.Item2 != lastReportedDamageLevel)
                {                    
                    // the damage has changed since we last reported it
                    if (worstDamage.Item1 != worstComponent || worstDamage.Item2 != worstDamageLevel)
                    {
                        Console.WriteLine("damage has changed...");
                        Console.WriteLine(worstDamage.Item1 + ", " + worstDamage.Item2);
                        // start the clock ticking and set the current damage to be the worst damage
                        timeWhenDamageLastChanged = currentGameState.Now;
                        worstComponent = worstDamage.Item1;
                        worstDamageLevel = worstDamage.Item2;
                    } 
                    else if (timeWhenDamageLastChanged.Add(timeToWaitForDamageToSettle) < currentGameState.Now)
                    {
                        Console.WriteLine("reporting ...");
                        Console.WriteLine(worstDamage.Item1 + ", " + worstDamage.Item2);
                        lastReportedComponent = worstComponent;
                        lastReportedDamageLevel = worstDamageLevel;
                        playWorstDamage();
                    }                 
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.AERO) || voiceMessage.Contains(SpeechRecogniser.BODY_WORK))
            {
                if (aeroDamage == DamageLevel.NONE)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoAeroDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (aeroDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereAeroDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (aeroDamage == DamageLevel.MINOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorAeroDamage, 0, null));
                }
                else if (aeroDamage == DamageLevel.TRIVIAL)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderJustAScratch, 0, null));
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.TRANSMISSION))
            {
                if (trannyDamage == DamageLevel.NONE)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoTransmissionDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderBustedTransmission, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereTransmissionDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.MINOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorTransmissionDamage, 0, null));
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.ENGINE))
            {
                if (engineDamage == DamageLevel.NONE)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoEngineDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderBustedEngine, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereEngineDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.MINOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorEngineDamage, 0, null));
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.SUSPENSION))
            {
                if (isMissingWheel)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMissingWheel, 0, null));
                    audioPlayer.closeChannel();
                }
                if (maxSuspensionDamage == DamageLevel.NONE && !isMissingWheel)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoSuspensionDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (maxSuspensionDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderBustedSuspension, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (maxSuspensionDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereSuspensionDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (maxSuspensionDamage == DamageLevel.MINOR && !isMissingWheel)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorSuspensionDamage, 0, null));
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.BRAKES))
            {
                if (maxBrakeDamage == DamageLevel.NONE)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoBrakeDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (maxBrakeDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderBustedBrakes, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (maxBrakeDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereBrakeDamage, 0, null));
                    audioPlayer.closeChannel();
                }
                else if (maxBrakeDamage == DamageLevel.MINOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorBrakeDamage, 0, null));
                    audioPlayer.closeChannel();
                }
            }
        }

        private Tuple<Component, DamageLevel> getWorstDamage()
        {
            if (engineDamage == DamageLevel.DESTROYED)
            {
                return new Tuple<Component, DamageLevel>(Component.ENGINE, DamageLevel.DESTROYED);
            }
            if (trannyDamage == DamageLevel.DESTROYED)
            {
                return new Tuple<Component, DamageLevel>(Component.TRANNY, DamageLevel.DESTROYED);
            }
            if (maxSuspensionDamage == DamageLevel.DESTROYED)
            {
                return new Tuple<Component, DamageLevel>(Component.SUSPENSION, DamageLevel.DESTROYED);
            }
            if (maxBrakeDamage == DamageLevel.DESTROYED)
            {
                return new Tuple<Component, DamageLevel>(Component.BRAKES, DamageLevel.DESTROYED);
            }
            if (aeroDamage == DamageLevel.DESTROYED)
            {
                return new Tuple<Component, DamageLevel>(Component.AERO, DamageLevel.DESTROYED);
            }
            if (engineDamage == DamageLevel.MAJOR)
            {
                return new Tuple<Component, DamageLevel>(Component.ENGINE, DamageLevel.MAJOR);
            }
            if (trannyDamage == DamageLevel.MAJOR)
            {
                return new Tuple<Component, DamageLevel>(Component.TRANNY, DamageLevel.MAJOR);
            }
            if (maxSuspensionDamage == DamageLevel.MAJOR)
            {
                return new Tuple<Component, DamageLevel>(Component.SUSPENSION, DamageLevel.MAJOR);
            }
            if (maxBrakeDamage == DamageLevel.MAJOR)
            {
                return new Tuple<Component, DamageLevel>(Component.BRAKES, DamageLevel.MAJOR);
            }
            if (maxBrakeDamage == DamageLevel.MAJOR)
            {
                return new Tuple<Component, DamageLevel>(Component.AERO, DamageLevel.MAJOR);
            }
            if (engineDamage == DamageLevel.MINOR)
            {
                return new Tuple<Component, DamageLevel>(Component.ENGINE, DamageLevel.MINOR);
            }
            if (trannyDamage == DamageLevel.MINOR)
            {
                return new Tuple<Component, DamageLevel>(Component.TRANNY, DamageLevel.MINOR);
            }
            if (maxSuspensionDamage == DamageLevel.MINOR)
            {
                return new Tuple<Component, DamageLevel>(Component.SUSPENSION, DamageLevel.MINOR);
            }
            if (maxBrakeDamage == DamageLevel.MINOR)
            {
                return new Tuple<Component, DamageLevel>(Component.BRAKES, DamageLevel.MINOR);
            }
            if (maxBrakeDamage == DamageLevel.MINOR)
            {
                return new Tuple<Component, DamageLevel>(Component.AERO, DamageLevel.MINOR);
            }
            if (engineDamage == DamageLevel.TRIVIAL)
            {
                return new Tuple<Component, DamageLevel>(Component.ENGINE, DamageLevel.TRIVIAL);
            }
            if (trannyDamage == DamageLevel.TRIVIAL)
            {
                return new Tuple<Component, DamageLevel>(Component.TRANNY, DamageLevel.TRIVIAL);
            }
            if (maxSuspensionDamage == DamageLevel.TRIVIAL)
            {
                return new Tuple<Component, DamageLevel>(Component.SUSPENSION, DamageLevel.TRIVIAL);
            }
            if (maxBrakeDamage == DamageLevel.TRIVIAL)
            {
                return new Tuple<Component, DamageLevel>(Component.BRAKES, DamageLevel.TRIVIAL);
            }
            if (maxBrakeDamage == DamageLevel.TRIVIAL)
            {
                return new Tuple<Component, DamageLevel>(Component.AERO, DamageLevel.TRIVIAL);
            }
            return new Tuple<Component, DamageLevel>(Component.NONE, DamageLevel.NONE);
        }

        private void playWorstDamage()
        {
            Boolean playMissingWheel = isMissingWheel;            
            if (worstComponent == Component.ENGINE)
            {
                if (worstDamageLevel == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedEngine, 0, this));
                    playMissingWheel = false;
                }
                else if (worstDamageLevel == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereEngineDamage, 0, this));
                }
                else if (worstDamageLevel == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorEngineDamage, 0, this));
                }
            }
            else if (worstComponent == Component.TRANNY)
            {
                if (worstDamageLevel == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedTransmission, 0, this));
                    playMissingWheel = false;
                }
                else if (worstDamageLevel == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereTransmissionDamage, 0, this));
                }
                else if (worstDamageLevel == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorTransmissionDamage, 0, this));
                }
            }
            else if (worstComponent == Component.SUSPENSION)
            {
                if (worstDamageLevel == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedSuspension, 0, this));
                    playMissingWheel = false;
                }
                else if (worstDamageLevel == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereSuspensionDamage, 0, this));
                }
                else if (worstDamageLevel == DamageLevel.MINOR && !isMissingWheel)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorSuspensionDamage, 0, this));
                }
            }
            else if (worstComponent == Component.BRAKES)
            {
                if (worstDamageLevel == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedBrakes, 0, this));
                    playMissingWheel = false;
                }
                else if (worstDamageLevel == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereBrakeDamage, 0, this));
                }
                else if (worstDamageLevel == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorBrakeDamage, 0, this));
                }
            }
            else if (worstComponent == Component.AERO)
            {
                if (worstDamageLevel == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereAeroDamage, 0, this));
                }
                else if (worstDamageLevel == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereAeroDamage, 0, this));
                }
                else if (worstDamageLevel == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorAeroDamage, 0, this));
                }
                else if (worstDamageLevel == DamageLevel.TRIVIAL)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderJustAScratch, 0, this));
                }
            }
            if (playMissingWheel)
            {
                audioPlayer.queueClip(new QueuedMessage(folderMissingWheel, 0, this));
            }
        }
    }
}
