using CrewChiefV3.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: actually finish this - one CornerData instance per data type (brake temp, tyre wear, ...)
namespace CrewChiefV3.Events
{
    public class CornerData {
        
        public Dictionary<Enum, Corners> cornersForEachStatus = new Dictionary<Enum, Corners>();

        public class EnumWithThresholds
        {
            public Enum e;
            public float lowerThreshold;
            public float upperThreshold;
            public EnumWithThresholds(Enum e, float lowerThreshold, float upperThreshold)
            {
                this.e = e;
                this.lowerThreshold = lowerThreshold;
                this.upperThreshold = upperThreshold;
            }
        }

        public enum Corners
        {
            FRONT_LEFT, FRONT_RIGHT, REAR_LEFT, REAR_RIGHT, FRONTS, REARS, LEFTS, RIGHTS, ALL, NONE
        }

        public Corners getCornersForStatus(Enum status)
        {
            if (cornersForEachStatus.ContainsKey(status))
            {
                return cornersForEachStatus[status];
            }
            else
            {
                return Corners.NONE;
            }
        }

        public static CornerData getCornerData(List<EnumWithThresholds> enumsWithThresholds, float leftFrontValue, float rightFrontValue, float leftRearValue, float rightRearValue)
        {
            CornerData cornerData = new CornerData();
            foreach (EnumWithThresholds enumWithThresholds in enumsWithThresholds)
            {
                if (leftFrontValue >= enumWithThresholds.lowerThreshold && leftFrontValue < enumWithThresholds.upperThreshold)
                {
                    if (rightFrontValue >= enumWithThresholds.lowerThreshold && rightFrontValue < enumWithThresholds.upperThreshold)
                    {
                        if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold &&
                            rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                        {
                            // it's 'whatever' all around
                            cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.ALL);
                        }
                        else
                        {
                            // front sides
                            cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.FRONTS);
                        }
                    }
                    else if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold)
                    {
                        cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.LEFTS);
                    }
                    else
                    {
                        cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.FRONT_LEFT);
                    }
                }
                else if (rightFrontValue >= enumWithThresholds.lowerThreshold && rightFrontValue < enumWithThresholds.upperThreshold)
                {
                    if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                    {
                        cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.RIGHTS);
                    }
                    else
                    {
                        cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.FRONT_RIGHT);
                    }
                }
                else if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold)
                {
                    if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                    {
                        cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.REARS);
                    }
                    else
                    {
                        cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.REAR_LEFT);
                    }
                }
                else if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                {
                    cornerData.cornersForEachStatus.Add(enumWithThresholds.e, Corners.REAR_RIGHT);
                }
            }
            return cornerData;
        }
    }
}
