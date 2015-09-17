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

        private String folderSevereTransmissionDamage = "damage_reporting/severe_transmission_damage";
        private String folderSevereEngineDamage = "damage_reporting/severe_engine_damage";
        private String folderSevereAeroDamage = "damage_reporting/severe_aero_damage";

        private String folderBustedTransmission = "damage_reporting/busted_transmission";
        private String folderBustedEngine = "damage_reporting/busted_engine";

        private String folderNoTransmissionDamage = "damage_reporting/no_transmission_damage";
        private String folderNoEngineDamage = "damage_reporting/no_engine_damage";
        private String folderNoAeroDamage = "damage_reporting/no_aero_damage";
        private String folderJustAScratch = "damage_reporting/trivial_aero_damage";

        Boolean playedMinorTransmissionDamage;
        Boolean playedMinorEngineDamage;
        Boolean playedMinorAeroDamage;
        Boolean playedSevereTransmissionDamage;
        Boolean playedSevereEngineDamage;
        Boolean playedSevereAeroDamage;
        Boolean playedBustedTransmission;
        Boolean playedBustedEngine;
        
        DamageLevel engineDamage;
        DamageLevel trannyDamage;
        DamageLevel aeroDamage;

        Boolean damageEnabled;

        public DamageReporting(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            damageEnabled = false;
        }

        public override void clearState()
        {
            playedMinorTransmissionDamage = false; playedMinorEngineDamage = false; playedMinorAeroDamage = false; playedSevereAeroDamage = false;
            playedSevereTransmissionDamage = false; playedSevereEngineDamage = false; playedBustedTransmission = false; playedBustedEngine = false;
            engineDamage = DamageLevel.NONE;
            trannyDamage = DamageLevel.NONE;
            aeroDamage = DamageLevel.NONE;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.CarDamageData.DamageEnabled)
            {
                aeroDamage = currentGameState.CarDamageData.OverallAeroDamage;
                trannyDamage = currentGameState.CarDamageData.OverallTransmissionDamage;
                engineDamage = currentGameState.CarDamageData.OverallEngineDamage;
                if (!playedBustedEngine && engineDamage == DamageLevel.DESTROYED)
                {
                    playedBustedEngine = true;
                    playedSevereEngineDamage = true;
                    playedMinorEngineDamage = true;
                    // if we've busted our engine, don't moan about other damage
                    playedBustedTransmission = true;
                    playedSevereTransmissionDamage = true;
                    playedMinorTransmissionDamage = true;
                    playedSevereAeroDamage = true;
                    playedMinorAeroDamage = true;
                    audioPlayer.queueClip(folderBustedEngine, 0, this);
                    audioPlayer.removeQueuedClip(folderSevereEngineDamage);
                }
                else if (!playedSevereEngineDamage && engineDamage == DamageLevel.MAJOR)
                {
                    playedSevereEngineDamage = true;
                    playedMinorEngineDamage = true;
                    audioPlayer.queueClip(folderSevereEngineDamage, 5, this);
                    audioPlayer.removeQueuedClip(folderMinorEngineDamage);
                }
                else if (!playedMinorEngineDamage && engineDamage == DamageLevel.MINOR)
                {
                    playedMinorEngineDamage = true;
                    audioPlayer.queueClip(folderMinorEngineDamage, 5, this);
                }

                if (!playedBustedTransmission && trannyDamage == DamageLevel.DESTROYED)
                {
                    playedBustedTransmission = true;
                    playedSevereTransmissionDamage = true;
                    playedMinorTransmissionDamage = true;
                    // if we've busted out transmission, don't moan about aero
                    playedSevereAeroDamage = true;
                    playedMinorAeroDamage = true;
                    audioPlayer.queueClip(folderBustedTransmission, 5, this);
                    audioPlayer.removeQueuedClip(folderSevereTransmissionDamage);
                }
                else if (!playedSevereTransmissionDamage && trannyDamage == DamageLevel.MAJOR)
                {
                    playedSevereTransmissionDamage = true;
                    playedMinorTransmissionDamage = true;
                    audioPlayer.queueClip(folderSevereTransmissionDamage, 5, this);
                    audioPlayer.removeQueuedClip(folderMinorTransmissionDamage);
                }
                else if (!playedMinorTransmissionDamage && trannyDamage == DamageLevel.MINOR)
                {
                    playedMinorTransmissionDamage = true;
                    audioPlayer.queueClip(folderMinorTransmissionDamage, 5, this);
                }

                if (!playedSevereAeroDamage && aeroDamage == DamageLevel.MAJOR)
                {
                    playedSevereAeroDamage = true;
                    playedMinorAeroDamage = true;
                    audioPlayer.queueClip(folderSevereAeroDamage, 5, this);
                    audioPlayer.removeQueuedClip(folderMinorAeroDamage);
                }
                else if (!playedMinorAeroDamage && aeroDamage == DamageLevel.MINOR)
                {
                    playedMinorAeroDamage = true;
                    audioPlayer.queueClip(folderMinorAeroDamage, 5, this);
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.AERO) || voiceMessage.Contains(SpeechRecogniser.BODY_WORK))
            {
                Console.WriteLine("Aero damage = " + aeroDamage);
                if (aeroDamage == DamageLevel.NONE)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderNoAeroDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (aeroDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderSevereAeroDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (aeroDamage == DamageLevel.MINOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderMinorAeroDamage, new QueuedMessage(0, null));
                }
                else if (aeroDamage == DamageLevel.TRIVIAL)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderJustAScratch, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.TRANSMISSION))
            {
                if (trannyDamage == DamageLevel.NONE)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderNoTransmissionDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderBustedTransmission, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderSevereTransmissionDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.MINOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderMinorTransmissionDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.ENGINE))
            {
                if (engineDamage == DamageLevel.NONE)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderNoEngineDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderBustedEngine, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderSevereEngineDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.MINOR)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderMinorEngineDamage, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
            }
        }
    }
}
