using CrewChiefV3.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: actually finish this - one CornerData instance per data type (brake temp, tyre wear, ...)
namespace CrewChiefV3.Events
{
    public class CornerData {
        
        public Dictionary<Enum, Corners> cornersData = new Dictionary<Enum, Corners>();

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
            FRONT_LEFT, FRONT_RIGHT, REAR_LEFT, REAR_RIGHT, FRONTS, REARS, LEFTS, RIGHTS, ALL
        }

        public void setCorners(List<EnumWithThresholds> enumsWithThresholds, float leftFrontValue, float rightFrontValue, float leftRearValue, float rightRearValue)
        {
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
                            cornersData.Add(enumWithThresholds.e, Corners.ALL);
                        }
                        else
                        {
                            // front sides
                            cornersData.Add(enumWithThresholds.e, Corners.FRONTS);
                        }
                    }
                    else if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold)
                    {
                        cornersData.Add(enumWithThresholds.e, Corners.LEFTS);
                    }
                    else
                    {
                        cornersData.Add(enumWithThresholds.e, Corners.FRONT_LEFT);
                    }
                }
                else if (rightFrontValue >= enumWithThresholds.lowerThreshold && rightFrontValue < enumWithThresholds.upperThreshold)
                {
                    if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                    {
                        cornersData.Add(enumWithThresholds.e, Corners.RIGHTS);
                    }
                    else
                    {
                        cornersData.Add(enumWithThresholds.e, Corners.FRONT_RIGHT);
                    }
                }
                else if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold)
                {
                    if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                    {
                        cornersData.Add(enumWithThresholds.e, Corners.REARS);
                    }
                    else
                    {
                        cornersData.Add(enumWithThresholds.e, Corners.REAR_LEFT);
                    }
                }
                else if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                {
                    cornersData.Add(enumWithThresholds.e, Corners.REAR_RIGHT);
                }
            }
        }
    }
}
