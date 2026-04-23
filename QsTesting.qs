include "qsr.qh";

enum ENUM
{
	First,
	Second
}

ENUM testing = ENUM.First

if (testing != ENUM.Second)
{
	printq @string ("\ntesting != ENUM.Second\n")
}