#pragma once

#include "Structures.h"
#include <intrin.h>

namespace NativeFaceDetector
{

	class Matrix
	{
		int *m_storage;
		Size m_size;

		Matrix(const Matrix& rhs);
		Matrix& operator=(const Matrix& rhs);
	public:
		Matrix(Size size);
		~Matrix();
	
		void resize(Size size);
		Size size() const { return m_size; }

		void prefetch(int i, int j) const { __prefetch(&m_storage[i * m_size.width + j]); }
		const int& operator()(int i, int j) const { return m_storage[i * m_size.width + j]; }
		int& operator()(int i, int j) { return m_storage[i * m_size.width + j]; }
		__forceinline int rectangleSum(int top, int right, int bottom, int left) const
		{
			const int *topRow    = m_storage + (top * m_size.width);
			const int *bottomRow = m_storage + (bottom * m_size.width);
			return bottomRow[right] - bottomRow[left] + topRow[left] - topRow[right];
		}
	};

}

