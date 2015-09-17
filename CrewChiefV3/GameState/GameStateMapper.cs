using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3.GameState
{
    interface GameStateMapper
    {
        GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState);

        void versionCheck(Object memoryMappedFileStruct);
        
        SessionType mapToSessionType(Object memoryMappedFileStruct);
    }
}
