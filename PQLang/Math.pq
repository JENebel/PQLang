fun Pow(a, b)
{
	if(b == 0 && a > 0) {return 1}
	if(b == 0 && a < 0) {return -1}
	if(a == 0) {return 0}

	d = Abs(b);

	c = a;
	for (d - 1)
	{
		c*=a
	}

	if(b > 0) {c} else {1/c}
}

fun Abs(a)
{
	if(a < 0) {-a} else {a}
}

fun Round(a)
{
	if(a % 1 < 0.5) {floor a} else {ceil a}
}