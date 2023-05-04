#if GODOT
using static Godot.Mathf;

#elif UNITY
using static UnityEngine.Mathf;
#endif

namespace Zeffyr.Tools
{
    // See visualizations of these functions on https://easings.net/
    public static class Easing
    {
        public static float Linear(float p)
        {
            return p;
        }

        public static class In
        {
            public static float Quad(float p)
            {
                return p * p;
            }

            public static float Cubic(float p)
            {
                return p * p * p;
            }

            public static float Quart(float p)
            {
                return p * p * p * p;
            }

            public static float Quint(float p)
            {
                return p * p * p * p * p;
            }

            public static float Elastic(float p)
            {
                return -(Pow(2, 10 * (p - 1f)) * Sin((p - 1.075f) * (Pi * 2) / 0.3f));
            }

            public static float Back(float p)
            {
                return p * p * (2.7f * p - 1.7f);
            }

            public static float Sine(float p)
            {
                return -Cos(p * (Pi * 0.5f)) + 1f;
            }

            public static float Expo(float p)
            {
                return Pow(2, (10 * (p - 1f)));
            }

            public static float Circ(float p)
            {
                return -(Sqrt(1f - (p * p)) - 1f);
            }

            public static float Bounce(float p)
            {
                return 1f - Out.Bounce(1f - p);
            }
        }

        public static class Out
        {
            public static float Quart(float p)
            {
                p = 1f - p;
                return 1f - (In.Quart(p));
            }

            public static float Quint(float p)
            {
                p = 1f - p;
                return 1f - (In.Quint(p));
            }


            public static float Cubic(float p)
            {
                p = 1f - p;
                return 1f - (In.Cubic(p));
            }


            public static float Elastic(float p)
            {
                p = 1f - p;
                return 1f - (In.Elastic(p));
            }


            public static float Back(float p)
            {
                p = 1f - p;
                return 1f - (In.Back(p));
            }


            public static float Sine(float p)
            {
                p = 1f - p;
                return 1f - (In.Sine(p));
            }

            public static float Expo(float p)
            {
                p = 1f - p;
                return 1f - (In.Expo(p));
            }


            public static float Quad(float p)
            {
                p = 1f - p;
                return 1f - (In.Quad(p));
            }


            public static float Circ(float p)
            {
                p = 1f - p;
                return 1f - (In.Circ(p));
            }

            public static float Bounce(float p)
            {
                if (p < 4 / 11.0f)
                {
                    return (121 * p * p) / 16.0f;
                }
                else if (p < 8 / 11.0f)
                {
                    return (363 / 40.0f * p * p) - (99 / 10.0f * p) + 17 / 5.0f;
                }
                else if (p < 9 / 10.0f)
                {
                    return (4356 / 361.0f * p * p) - (35442 / 1805.0f * p) + 16061 / 1805.0f;
                }
                else
                {
                    return (54 / 5.0f * p * p) - (513 / 25.0f * p) + 268 / 25.0f;
                }
            }
        }

        public static class InOut
        {
            public static float Quad(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Quad(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Quad(p))) + 0.5f;
                }
            }

            public static float Cubic(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Cubic(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Cubic(p))) + 0.5f;
                }
            }

            public static float Quart(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Quart(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Quart(p))) + 0.5f;
                }
            }

            public static float Quint(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Quint(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Quint(p))) + 0.5f;
                }
            }

            public static float Elastic(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Elastic(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Elastic(p))) + 0.5f;
                }
            }

            public static float Expo(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Expo(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Expo(p))) + 0.5f;
                }
            }

            public static float Back(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Back(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Back(p))) + 0.5f;
                }
            }

            public static float Circ(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Circ(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Circ(p))) + 0.5f;
                }
            }

            public static float Sine(float p)
            {
                p = p * 2;
                if (p < 1f)
                {
                    return 0.5f * (In.Sine(p));
                }
                else
                {
                    p = 2f - p;
                    return 0.5f * (1f - (In.Sine(p))) + 0.5f;
                }
            }

            public static float Bounce(float p)
            {
                if (p < 0.5f)
                {
                    return 0.5f * In.Bounce(p * 2f);
                }
                else
                {
                    return 0.5f * Out.Bounce(p * 2f - 1f) + 0.5f;
                }
            }
        }
    }
}