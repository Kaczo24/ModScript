let a = InputN();
Printl(fac(a));



function fac(x)
{
	if(x == 0)
		return 1;
	let z = this(x - 1) * x;
	Printl(z + " : " + x);
	return z;
}