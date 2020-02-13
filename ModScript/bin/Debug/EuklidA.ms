let a = InputN();
let b = InputN();
Printl(Euq(a, b));



function Euq(x, y)
{
	if(x == y)
		return x;
	if(x > y)
	{
		let z = this(x - y, y);
		Printl(z + " : " + x + " : " + y);
		return z;
	
	}
	let z = this(x, y - x);
	Printl(z + " : " + x + " : " + y);
	return z;
}