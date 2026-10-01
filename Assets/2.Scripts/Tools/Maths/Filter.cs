namespace DT.Tools
{
    /// <summary>
    /// Simple class to filter values
    /// </summary>
    public class Filter
    {

        uint filterSize = 10;
        uint index = 0;


        // Float
        float[] rawData;
        float filtered;

        // Array
        bool isArray = false;

        float[][] rawDataArray;
        float[] filteredArray;


        public Filter(float initValue, uint filterSize)
        {
            this.filterSize = filterSize;
            rawData = new float[filterSize];
            for (int i = 0; i < filterSize; i++)
            {
                rawData[i] = initValue;
            }
            index = 0;
        }

        public Filter(float[] initValue, uint filterSize)
        {
            isArray = true;

            this.filterSize = filterSize;
            rawDataArray = new float[filterSize][];
            filteredArray = new float[initValue.Length];
            for (int i = 0; i < filterSize; i++)
            {
                rawDataArray[i] = initValue;
            }
            index = 0;
        }

        public float filter(float value)
        {
            if (isArray)
            {
                return value;
            }

            // Set data to filter array
            rawData[index] = value;
            index++;

            filtered = 0;
            // compute result
            for (int i = 0; i < filterSize; i++)
            {
                filtered += rawData[i];
            }
            filtered = filtered / filterSize;

            return filtered;
        }


        public float[] filter(float[] value)
        {
            if (!isArray)
            {
                return value;
            }


            // Set data to filter array
            for (int i = 0; i < rawDataArray[0].Length; i++)
            {
                rawDataArray[index] = value;
            }
            index++;
            if (index == filterSize)
            {
                index = 0;
            }

            filteredArray = new float[rawDataArray[0].Length];
            // compute result
            for (int i = 0; i < filterSize; i++)
            {

                for (int j = 0; j < rawDataArray[0].Length; j++)
                {
                    filteredArray[j] += rawDataArray[i][j] / filterSize;
                }
            }



            return filteredArray;
        }

    }

}

