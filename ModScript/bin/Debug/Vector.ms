
function Vector(X, Y)
{
	public let x = X;
	public let y = Y;
	public function mag() => Sqrt(x^2 + y^2);
	

	return this;
}

Vector << mult = function(M) {
	x *= M;
	y *= M;
}

function Vector3(X, Y, Z) 
{
	Super << Vector(X, Y);
	
	public let z = Z;
	mag = function() => Sqrt(x^2 + y^2 + z^2);

	return this;
}


Vector3 << mult = function(M) {
	x *= M;
	y *= M;
	z *= M;
}