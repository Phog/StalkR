#include "pch.h"
#include "Tree.h"

using namespace NativeFaceDetector;

float Tree::getVal(const Matrix &grayImage, const Matrix &squares, int i, int j) const
{
    const Feature *curNode = &m_features[0];
    for (;;)
    {

        if (curNode->chooseLeft(grayImage, squares, i, j))
        {
            if (curNode->leftNode() <= 0)
                return curNode->leftVal();

            curNode = &m_features[curNode->leftNode() - 1];
        }
        else
        {
            if (curNode->rightNode() <= 0)
                return curNode->rightVal();

			curNode = &m_features[curNode->rightNode() - 1];
        }
    }
}
