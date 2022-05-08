class List() {
	arr = [4];
	count = 0;

	fun add(item) {
		arr[count] = item;
			count++;

		if (arr.length == count) {
			//Copy elements to new array
			newArr = [arr.length * 2];

			for(i = 0; i < count; i++) {
				newArr[i] = arr[i]
			}
			arr = newArr;
		}
	}

	fun removeAt(index) {
		if (index < 0 || index >= count) { error("Index out of bounds") };

		for (i = index; i < count; i++) {
			if( i != 0) {
				arr[index + i - 1] = arr[index + i];
			}
		}

		count--;

		if (count < (arr.length / 4) && count != 0) {
			//Halve capacity
			newArr = [floor(arr.length / 2)];

			for(i = 0; i < count; i++) {
				newArr[i] = arr[i]
			}
			arr = newArr;
		}
	}

	fun set(index, value) {
		if (index < 0 || index >= count) { error("Index out of bounds") };
		arr[index] = value
	}

	fun get(index) {
		if (index < 0 || index >= count) { error("Index out of bounds") };
		arr[index]
	}

	fun size() {
		count
	}

	fun toString() {
		str = "[";
		if (count > 1) {
			for (i = 0; i < count - 1; i++) {
				str += arr[i] + ", "
			}
		}
		if(count > 0) { str += arr[count - 1] };
		str += "]";

		str
	}
}