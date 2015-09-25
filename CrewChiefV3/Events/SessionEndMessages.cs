using CrewChiefV3.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3.Events
{
    class SessionEndMessages
    {
        private String folderPodiumFinish = "lap_counter/podium_finish";

        private String folderWonRace = "lap_counter/won_race";

        private String folderFinishedRace = "lap_counter/finished_race";

        private String folderFinishedRaceLast = "lap_counter/finished_race_last";

        private String folderEndOfSession = "lap_counter/end_of_session";

        private String folderEndOfSessionPole = "lap_counter/end_of_session_pole";

        private AudioPlayer audioPlayer;

        private DateTime lastFinishMessageTime = DateTime.MinValue;

        public SessionEndMessages(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public void trigger(float sessionRunningTime, SessionType sessionType, SessionPhase lastSessionPhase, 
            int finishPosition, int numCars, int completedLaps, DateTime now, Boolean isDisqualified)
        {
            if (sessionType == SessionType.Race)
            {
                if (sessionRunningTime > 60 || completedLaps > 0)
                {
                    if (lastSessionPhase == SessionPhase.Finished)
                    {
                        // only play session end message for races if we've actually finished, not restarted
                        playFinishMessage(sessionType, finishPosition, numCars, now, isDisqualified);
                    }
                    else
                    {
                        Console.WriteLine("skipping race session end message because the previous phase wasn't Finished");
                    }
                }
                else
                {
                    Console.WriteLine("skipping race session end message because it didn't run for a lap or a minute");
                }
            }
            else if (sessionType == SessionType.Practice || sessionType == SessionType.Qualify)
            {
                if (sessionRunningTime > 60)
                {
                    if (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Checkered)
                    {
                        playFinishMessage(sessionType, finishPosition, numCars, now, false);
                    }
                    else
                    {
                        Console.WriteLine("skipping non-race session end message because the previous phase wasn't green, finished, or checkered");
                    }
                }
                else
                {
                    Console.WriteLine("skipping non-race session end message because the session didn't run for a minute");
                }
            }
        }

        public void playFinishMessage(SessionType sessionType, int position, int numCars, DateTime now, Boolean isDisqualified)
        {
            if (lastFinishMessageTime.Add(TimeSpan.FromSeconds(2)) < now)
            {
                audioPlayer.suspendPearlsOfWisdom();
                lastFinishMessageTime = now;
                if (position < 1)
                {
                    Console.WriteLine("Session finished but position is < 1");
                }
                else if (sessionType == SessionType.Race)
                {
                    Boolean isLast = position == numCars;
                    if (isDisqualified) 
                    {
                        audioPlayer.queueClip(new QueuedMessage(Penalties.folderDisqualified, 0, null));
                    }
                    else if (position == 1)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderWonRace, 0, null));
                    }
                    else if (position < 4)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderPodiumFinish, 0, null));
                    }
                    else if (position >= 4 && !isLast)
                    {
                        audioPlayer.queueClip(new QueuedMessage(Position.folderStub + position, 0, null));
                        audioPlayer.queueClip(new QueuedMessage(folderFinishedRace, 0, null));
                    }
                    else if (isLast)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderFinishedRaceLast, 0, null));
                    }
                }
                else
                {
                    if (sessionType == SessionType.Qualify && position == 1)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderEndOfSessionPole, 0, null));
                    }
                    else
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderEndOfSession, 0, null));
                        if (position > 24)
                        {
                            audioPlayer.queueClip(new QueuedMessage("finish_position", AbstractEvent.MessageContents(Position.folderStub, QueuedMessage.folderNameNumbersStub + position), 0, null));
                        }
                        else
                        {
                            audioPlayer.queueClip(new QueuedMessage(Position.folderStub + position, 0, null));
                        }
                    }
                }
            }
        }
    }
}
