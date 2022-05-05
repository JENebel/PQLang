n = 0;
a = 0;

while (n < 100000) {
	if((n != 0) && (n != 1)) {
		i = (£n);
		g = true;
		while(i > 1){
			if((!g) || ((n % i) == 0)) {
				g = false
			};
			i--;
		};
		if(g) {
			a++;
		}
	};
	n++;
};

a