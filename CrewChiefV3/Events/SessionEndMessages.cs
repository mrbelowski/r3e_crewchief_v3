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

        public void trigger(float sessionRunningTime, SessionType sessionType, SessionPhase lastSessionPhase, int finishPosition, int numCars)
        {
            if (sessionRunningTime > 60)
            {
                if (sessionType == SessionType.Race) 
                {
                    if (lastSessionPhase == SessionPhase.Finished)
                    {
                        // only play session end message for races if we've actually finished, not restarted
                        playFinishMessage(sessionType, finishPosition, numCars);
                    } 
                }
                else if (sessionType == SessionType.Practice || sessionType == SessionType.Qualify)
                {
                    if (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Checkered)
                    {
                        playFinishMessage(sessionType, finishPosition, numCars);
                    }
                }
            }
        }

        public void playFinishMessage(SessionType sessionType, int position, int numCars)
        {
            if (lastFinishMessageTime.Add(TimeSpan.FromSeconds(2)) < DateTime.Now)
            {
                lastFinishMessageTime = DateTime.Now;
                if (position < 1)
                {
                    Console.WriteLine("Session finished but position is < 1");
                }
                else if (sessionType == SessionType.Race)
                {
                    Boolean isLast = position == numCars;
                    if (position == 1)
                    {
                        audioPlayer.queueClip(folderWonRace, 0, null);
                    }
                    else if (position < 4)
                    {
                        audioPlayer.queueClip(folderPodiumFinish, 0, null);
                    }
                    else if (position >= 4 && !isLast)
                    {
                        audioPlayer.queueClip(Position.folderStub + position, 0, null);
                        audioPlayer.queueClip(folderFinishedRace, 0, null);
                    }
                    else if (isLast)
                    {
                        audioPlayer.queueClip(folderFinishedRaceLast, 0, null);
                    }
                }
                else
                {
                    if (sessionType == SessionType.Qualify && position == 1)
                    {
                        audioPlayer.queueClip(folderEndOfSessionPole, 0, null);
                    }
                    else
                    {
                        audioPlayer.queueClip(folderEndOfSession, 0, null, PearlsOfWisdom.PearlType.NONE, 0);
                        audioPlayer.queueClip(Position.folderStub + position, 0, null);
                    }
                }
            }
        }
    }
}
