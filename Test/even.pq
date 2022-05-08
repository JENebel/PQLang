a = 123;

fun even(x) {
	if (x == 0) {true} 
	else {odd(x-1)}
};
fun odd(x) {
	if (x == 0) {false} 
	else {even(x-1)}
};

even(a)